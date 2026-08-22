using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine.DryRun;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Runs;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// Represents a workflow application that can be run.
/// </summary>
public sealed class WorkflowApplication
{
    private readonly WorkflowRegistry _workflows;
    private readonly RuntimeRegistry _runtimes;
    private readonly IReadOnlyList<ServiceDescriptor> _services;
    private readonly DecoratorRegistry _decorators;
    private readonly string _projectFileName;

    internal WorkflowApplication(
        WorkflowRegistry workflows,
        RuntimeRegistry runtimes,
        IReadOnlyList<ServiceDescriptor> services,
        DecoratorRegistry decorators,
        string projectFileName
    )
    {
        _workflows = workflows;
        _runtimes = runtimes;
        _services = services;
        _decorators = decorators;
        _projectFileName = projectFileName;
    }

    /// <summary>
    /// Creates a builder for configuring a workflow application.
    /// </summary>
    public static WorkflowApplicationBuilder CreateBuilder() => new();

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
    /// Runs the requested job of the resolved workflow.
    /// </summary>
    /// <param name="workflow">What <see cref="SelectWorkflow"/> made of the directory.</param>
    /// <param name="args">What the command line asked of the job.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<ExitCode> Run(Result<SelectedWorkflow> workflow, RunJobArgs args, CancellationToken ct) =>
        Run(workflow, args, Environment.GetEnvironmentVariable, ct);

    /// <summary>
    /// Runs the requested job against the given environment.
    /// </summary>
    internal async Task<ExitCode> Run(Result<SelectedWorkflow> workflow, RunJobArgs args, Func<string, string?> environment, CancellationToken ct)
    {
        var runtime = _runtimes.Detect(environment);
        if (runtime.IsError)
        {
            // Failed before we can get a runtime-specific logger, so use the default one.
            return ConfigurationError(EngineConsole(args.LogLevel), runtime.Errors);
        }

        // From now on, we have a fancy forge-specific logger that can handle error reporting and escaping and stuff.
        var console = runtime.Value.CreateConsole(args.LogLevel);
        if (workflow.IsError)
        {
            return ConfigurationError(console, workflow.Errors);
        }

        var resolved = workflow.Value;

        // Make sure the job is valid.
        var jobs = resolved.Workflow.Jobs;
        var job = jobs.FirstOrDefault(j => j.Name == args.Job);
        if (job is null)
        {
            return ConfigurationError(console, [
                Result.Error($"The {resolved.Workflow.Label} workflow has no job named '{args.Job}'."),
                Result.Error($"Known jobs: {string.Join(", ", jobs.Select(j => j.Name))}.")
            ]);
        }

        // Check the project file exists if it's required.
        if (job.RequiresProject && resolved.MissingProjectReason is { } noProject)
        {
            return ConfigurationError(console, [noProject]);
        }

        // Check for any missing arguments.
        if (Missing(job, args.Arguments) is { Count: > 0 } missing)
        {
            return ConfigurationError(console, missing);
        }

        var builder = new WorkflowRunBuilder(resolved.Project, runtime.Value, console)
            .WithWorkflow(resolved)
            .WithDryRun(args.DryRun)
            .WithAutoApprove(args.AutoApprove)
            .WithArguments(args.Arguments)
            .WithServices(_services)
            .WithDecorators(_decorators);

        var run = builder.Build(job);
        if (run.IsError)
        {
            return ConfigurationError(console, run.Errors);
        }

        using var _ = run.Value;
        return await run.Value.Run(ct);
    }

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

    /// <summary>
    /// The arguments a job declared as required and wasn't given.
    /// </summary>
    private static List<Error> Missing(IJob job, JobArguments arguments) =>
    [
        .. job.Arguments
            .Where(argument => argument.Required && !arguments.Arguments.Contains(argument))
            .Select(argument => Result.Error($"The {job.Name} job needs '{argument.Name}': {argument.Description}"))
    ];

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
