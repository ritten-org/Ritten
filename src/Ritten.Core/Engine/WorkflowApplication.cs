using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Runs;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// Represents a workflow application that can be run.
/// </summary>
/// <remarks>
/// Every service is registered and validated when the application is built; each run resolves them in a scope of
/// its own, which holds what the run is about.
/// </remarks>
public sealed class WorkflowApplication : IDisposable, IAsyncDisposable
{
    private readonly WorkflowSet _workflows;
    private readonly ServiceProvider _services;
    private readonly DetectRuntimeResult _runtime;
    private readonly string _projectFileName;

    internal WorkflowApplication(WorkflowSet workflows, ServiceProvider services, DetectRuntimeResult runtime, string projectFileName)
    {
        _workflows = workflows;
        _services = services;
        _runtime = runtime;
        _projectFileName = projectFileName;
    }

    /// <summary>
    /// Creates a builder for configuring a workflow application.
    /// </summary>
    public static WorkflowApplicationBuilder CreateBuilder() => new();

    /// <summary>
    /// The workflows the application can run, in registration order.
    /// </summary>
    public IReadOnlyList<IWorkflow> Workflows => _workflows.Workflows;

    /// <summary>
    /// Every service the application registered, each run resolving its own in a scope.
    /// </summary>
    internal IServiceProvider Services => _services;

    /// <summary>
    /// Resolves what the given directory asks Ritten to be: the project file it declares — or
    /// hasn't written yet — and the workflow that follows from it. Done before any job is chosen,
    /// since which jobs there are to choose from is the answer.
    /// </summary>
    /// <param name="directory">The directory the tool was invoked in.</param>
    /// <param name="workflow">
    /// The workflow to run, for a repository whose project file doesn't declare one. Refused when
    /// the project declares a different one, so a name given is never quietly discarded.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Result<SelectedWorkflow>> SelectWorkflow(string directory, string? workflow = null, CancellationToken ct = default)
    {
        var known = Result.Error($"Known workflows: {string.Join(", ", _workflows.Names)}.");

        var resolved = await RittenProject.Resolve(directory, _projectFileName, ct);
        if (resolved.IsError)
        {
            return new Result<SelectedWorkflow>(resolved.Errors);
        }

        var project = resolved.Value;
        var declared = project.GetWorkflowName();
        if (declared.IsSuccess)
        {
            if (_workflows.Find(declared.Value) is not { } workflow2)
            {
                return new Result<SelectedWorkflow>([
                    Result.Error($"'{project.FilePath}' declares the unknown workflow '{declared.Value}'."),
                    known
                ]);
            }

            // Error if manually specified workflow clashes with project workflow.
            return workflow is { Length: > 0 } named && !string.Equals(named, workflow2.Name, StringComparison.OrdinalIgnoreCase)
                ? new Result<SelectedWorkflow>([
                    Result.Error($"'{project.FilePath}' declares the {workflow2.Label} workflow, so '{named}' can't be run here."),
                    Result.Error("Change the \"workflow\" key to run a different one.")
                ])
                : new SelectedWorkflow(workflow2, project);
        }

        // No project file declared in the directory, check the user args.
        var undeclared = declared.Errors.First();
        if (workflow is { Length: > 0 } name)
        {
            return _workflows.Find(name) is { } named
                ? new SelectedWorkflow(named, project) { MissingProjectReason = undeclared }
                : new Result<SelectedWorkflow>([Result.Error($"There is no workflow named '{name}'."), known]);
        }

        if (await _workflows.IsCompatible(new PhysicalDirectory(directory), ct) is { } recognised)
        {
            return new SelectedWorkflow(recognised.Workflow, project, recognised.Reason) { MissingProjectReason = undeclared };
        }

        return new Result<SelectedWorkflow>([undeclared, known]);
    }

    /// <summary>
    /// Runs the requested job of the resolved workflow, with arguments read from the project file and the
    /// command line.
    /// </summary>
    /// <param name="workflow">What <see cref="SelectWorkflow"/> made of the directory.</param>
    /// <param name="args">What the command line asked of the job.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ExitCode> Run(Result<SelectedWorkflow> workflow, RunJobArgs args, CancellationToken ct)
    {
        var console = _runtime.CreateConsole(args.LogLevel);
        if (workflow.IsError)
        {
            return ConfigurationError(console, workflow.Errors);
        }

        var resolved = workflow.Value;
        var jobs = resolved.Workflow.Jobs;
        var job = jobs.FirstOrDefault(j => j.Name == args.Job);
        if (job is null)
        {
            return ConfigurationError(console, [
                Result.Error($"The {resolved.Workflow.Label} workflow has no job named '{args.Job}'."),
                Result.Error($"Known jobs: {string.Join(", ", jobs.Select(j => j.Name))}.")
            ]);
        }

        var arguments = ReadArguments(resolved, job, args, console);
        if (arguments.IsError)
        {
            return ConfigurationError(console, arguments.Errors);
        }

        return await Run(resolved, job, arguments.Value, args.RunOptions, console, ct);
    }

    /// <summary>
    /// Reads and judges the arguments a job would run with, from the project file and the command line, before
    /// anything is assembled.
    /// </summary>
    /// <param name="workflow">The workflow, and the project it runs in.</param>
    /// <param name="job">The job.</param>
    /// <param name="args">What the command line asked of the job.</param>
    /// <param name="log">Where warnings go.</param>
    internal Result<object> ReadArguments(SelectedWorkflow workflow, IJob job, RunJobArgs args, IWorkflowLog log)
    {
        // Check the project file exists if it's required.
        if (job.RequiresProject && workflow.MissingProjectReason is { } noProject)
        {
            return noProject;
        }

        var arguments = ArgumentsModel.For(job.ArgumentsType).Read(workflow.Project, args.Options);
        if (arguments.IsError)
        {
            return arguments;
        }

        return Validate(workflow, job, arguments.Value, args.DryRun, log) is { Count: > 0 } invalid ? new Result<object>(invalid) : arguments;
    }

    /// <summary>
    /// Runs a job of one of the application's workflows with the given arguments, in the given directory.
    /// </summary>
    /// <param name="workflow">The workflow the job belongs to.</param>
    /// <param name="job">The job to run.</param>
    /// <param name="arguments">The job's arguments, of its <see cref="IJob.ArgumentsType"/>.</param>
    /// <param name="directory">The directory the job runs in, as its project's root.</param>
    /// <param name="options">How to run it.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ExitCode> Run(IWorkflow workflow, IJob job, object arguments, string directory, RunOptions options, CancellationToken ct = default)
    {
        var console = _runtime.CreateConsole(options.LogLevel);
        if (!_workflows.Workflows.Contains(workflow))
        {
            return ConfigurationError(console, [Result.Error($"The {workflow.Label} workflow isn't one this application runs.")]);
        }

        if (!workflow.Jobs.Contains(job))
        {
            return ConfigurationError(console, [Result.Error($"The {workflow.Label} workflow has no {job.Name} job.")]);
        }

        if (!job.ArgumentsType.IsInstanceOfType(arguments))
        {
            return ConfigurationError(console, [Result.Error($"The {job.Name} job runs with {job.ArgumentsType.Name}, not {arguments.GetType().Name}.")]);
        }

        var selected = new SelectedWorkflow(workflow, RittenProject.At(directory, _projectFileName));
        if (Validate(selected, job, arguments, options.DryRun, console) is { Count: > 0 } invalid)
        {
            return ConfigurationError(console, invalid);
        }

        return await Run(selected, job, arguments, options, console, ct);
    }

    /// <summary>
    /// Runs a job of one of the application's workflows with the given arguments, in the given directory.
    /// </summary>
    /// <param name="workflow">The workflow the job belongs to.</param>
    /// <param name="job">The job to run.</param>
    /// <param name="arguments">The job's arguments.</param>
    /// <param name="directory">The directory the job runs in, as its project's root.</param>
    /// <param name="options">How to run it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <typeparam name="TArguments">The job's arguments type.</typeparam>
    public Task<ExitCode> Run<TArguments>(IWorkflow workflow, Job<TArguments> job, TArguments arguments, string directory, RunOptions options, CancellationToken ct = default)
        where TArguments : class =>
        Run(workflow, (IJob)job, arguments, directory, options, ct);

    /// <summary>
    /// The jobs a command line should offer in the given project directory.
    /// </summary>
    /// <param name="directory">The directory the tool was invoked in.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IReadOnlyList<IJob>> ResolveJobs(string directory, CancellationToken ct = default)
    {
        var project = await RittenProject.Resolve(directory, _projectFileName, ct);
        if (project.IsSuccess
            && project.Value.GetWorkflowName() is { IsSuccess: true } name
            && _workflows.Find(name.Value) is { } workflow)
        {
            return workflow.Jobs;
        }

        return [.. _workflows.Workflows.SelectMany(w => w.Jobs).DistinctBy(j => j.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public void Dispose() => _services.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _services.DisposeAsync();

    /// <summary>
    /// Runs the job in a scope of its own.
    /// </summary>
    private async Task<ExitCode> Run(SelectedWorkflow workflow, IJob job, object arguments, RunOptions options, IWorkflowConsole console, CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RunState>().Start(new Run(workflow, job, arguments, options, console));

        IWorkflowRunner runner;
        try
        {
            runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return ConfigurationError(console, [Result.Error(exception.Message, exception)]);
        }

        var result = await runner.Run(ct);
        return result.ExitCode;
    }

    // Nothing has been written for a project that doesn't exist yet, so there is nothing to judge.
    private IReadOnlyList<Error> Validate(SelectedWorkflow workflow, IJob job, object arguments, bool dryRun, IWorkflowLog log) =>
        workflow.Project.IsSynthetic ? [] : job.Validate(arguments, _runtime.Environment, dryRun, log, workflow.Project.FileName);

    // Before a runtime is selected there's nothing to ask for a console, so errors that early
    // print through the engine's own renderer.
    internal static IWorkflowConsole EngineConsole(WorkflowLogLevel level = WorkflowLogLevel.Detail) =>
        Reporting.EngineConsole.Create(level);

    private static ExitCode ConfigurationError(IWorkflowLog log, IEnumerable<Error> errors)
    {
        log.Errors(errors);
        return ExitCode.ConfigurationError;
    }
}
