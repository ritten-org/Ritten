using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runtimes;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Core;

/// <summary>
/// Represents a pipeline application that can be run.
/// </summary>
public sealed class PipelineApplication
{
    private readonly PipelineRegistry _pipelines;
    private readonly RuntimeRegistry _runtimes;
    private readonly IReadOnlyList<ServiceDescriptor> _services;

    internal PipelineApplication(PipelineRegistry pipelines, RuntimeRegistry runtimes, IReadOnlyList<ServiceDescriptor> services)
    {
        _pipelines = pipelines;
        _runtimes = runtimes;
        _services = services;
    }

    /// <summary>
    /// Creates a builder for configuring a pipeline application.
    /// </summary>
    public static PipelineApplicationBuilder CreateBuilder() => new();

    /// <summary>
    /// Runs the requested job of whichever registered pipeline the resolved project declares,
    /// returning its exit code.
    /// </summary>
    /// <param name="args">What the command line asked for.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<int> Run(RunJobArgs args, CancellationToken ct) =>
        Run(args, Environment.GetEnvironmentVariable, ct);

    /// <summary>
    /// Runs the requested job against the given directory and environment.
    /// </summary>
    internal async Task<int> Run(RunJobArgs args, Func<string, string?> environment, CancellationToken ct)
    {
        var runtime = _runtimes.Detect(environment);
        if (runtime.IsError)
        {
            // Failed before we can get a runtime-specific logger, so use the default one.
            return ConfigurationError(EngineConsole(args.LogLevel), runtime.Errors);
        }

        // From now on, we have a fancy forge-specific logger that can handle error reporting and escaping and stuff.
        var console = runtime.Value.CreateConsole(args.LogLevel);

        var project = await RittenProject.Resolve(args.Directory, ct);
        if (project.IsError)
        {
            return ConfigurationError(console, project.Errors);
        }

        var knownPipelines = Result.Error($"Known pipelines: {string.Join(", ", _pipelines.Names)}.");
        var name = project.Value.GetPipelineName();
        if (name.IsError)
        {
            return ConfigurationError(console, [.. name.Errors, knownPipelines]);
        }

        if (_pipelines.Find(name.Value) is not { } pipeline)
        {
            return ConfigurationError(console, [Result.Error($"'{project.Value.FilePath}' declares the unknown pipeline '{name.Value}'."), knownPipelines]);
        }

        // The job is picked out of the static model before settings are parsed: a typo'd
        // command shouldn't need a valid configuration to be diagnosed.
        var jobs = pipeline.Jobs;
        var declared = jobs.FirstOrDefault(j => j.Name == args.Job);
        if (declared is null)
        {
            return ConfigurationError(console, [
                Result.Error($"The {pipeline.Label} pipeline has no job named '{args.Job}'."),
                Result.Error($"Known jobs: {string.Join(", ", jobs.Select(j => j.Name))}.")
            ]);
        }

        var builder = new PipelineRunBuilder(project.Value, runtime.Value, console)
            .WithPipelineLabel(pipeline.Label)
            .WithDryRun(args.DryRun)
            .WithAutoApprove(args.AutoApprove)
            .WithServices(_services);

        var run = builder.Build(declared);
        if (run.IsError)
        {
            return ConfigurationError(console, run.Errors);
        }

        using var _ = run.Value;
        return await run.Value.Run(ct);
    }

    // Before a runtime is selected there's nothing to ask for a console, so errors that early
    // print through the engine's own renderer.
    internal static SpectrePipelineConsole EngineConsole(PipelineLogLevel level = PipelineLogLevel.Detail) =>
        new(AnsiConsole.Console, level);

    private static int ConfigurationError(IPipelineLog log, IEnumerable<Error> errors)
    {
        log.Errors(errors);
        return PipelineExitCodes.ConfigurationError;
    }
}
