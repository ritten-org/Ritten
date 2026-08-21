using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine.DryRun;
using Ritten.Engine.Runs;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;
using Spectre.Console;

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
    /// Runs the requested job of whichever registered workflow the resolved project declares,
    /// returning its exit code.
    /// </summary>
    /// <param name="args">What the command line asked for.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<ExitCode> Run(RunJobArgs args, CancellationToken ct) =>
        Run(args, Environment.GetEnvironmentVariable, ct);

    /// <summary>
    /// Runs the requested job against the given directory and environment.
    /// </summary>
    internal async Task<ExitCode> Run(RunJobArgs args, Func<string, string?> environment, CancellationToken ct)
    {
        var runtime = _runtimes.Detect(environment);
        if (runtime.IsError)
        {
            // Failed before we can get a runtime-specific logger, so use the default one.
            return ConfigurationError(EngineConsole(args.LogLevel), runtime.Errors);
        }

        // From now on, we have a fancy forge-specific logger that can handle error reporting and escaping and stuff.
        var console = runtime.Value.CreateConsole(args.LogLevel);

        var project = await RittenProject.Resolve(args.Directory, _projectFileName, ct);
        if (project.IsError)
        {
            return ConfigurationError(console, project.Errors);
        }

        var knownWorkflows = Result.Error($"Known workflows: {string.Join(", ", _workflows.Names)}.");
        var name = project.Value.GetWorkflowName();
        if (name.IsError)
        {
            return ConfigurationError(console, [.. name.Errors, knownWorkflows]);
        }

        if (_workflows.Find(name.Value) is not { } workflow)
        {
            return ConfigurationError(console, [Result.Error($"'{project.Value.FilePath}' declares the unknown workflow '{name.Value}'."), knownWorkflows]);
        }

        // The job is picked out of the static model before settings are parsed: a typo'd
        // command shouldn't need a valid configuration to be diagnosed.
        var jobs = workflow.Jobs;
        var declared = jobs.FirstOrDefault(j => j.Name == args.Job);
        if (declared is null)
        {
            return ConfigurationError(console, [
                Result.Error($"The {workflow.Label} workflow has no job named '{args.Job}'."),
                Result.Error($"Known jobs: {string.Join(", ", jobs.Select(j => j.Name))}.")
            ]);
        }

        // Judged before anything is assembled, so a job that wasn't given what it needs says so
        // beside "unknown job" rather than failing several steps in. Nothing checks the names:
        // a value can only be supplied through a declaration, so a stray one can't be expressed.
        if (Missing(declared, args.Arguments) is { Count: > 0 } missing)
        {
            return ConfigurationError(console, missing);
        }

        var builder = new WorkflowRunBuilder(project.Value, runtime.Value, console)
            .WithWorkflowLabel(workflow.Label)
            .WithDryRun(args.DryRun)
            .WithAutoApprove(args.AutoApprove)
            .WithArguments(args.Arguments)
            .WithServices(_services)
            .WithDecorators(_decorators);

        var run = builder.Build(declared);
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
    internal static SpectreWorkflowConsole EngineConsole(WorkflowLogLevel level = WorkflowLogLevel.Detail) =>
        new(AnsiConsole.Console, level);

    private static ExitCode ConfigurationError(IWorkflowLog log, IEnumerable<Error> errors)
    {
        log.Errors(errors);
        return ExitCode.ConfigurationError;
    }
}
