using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runner;
using Ritten.Core.Runtimes;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Core;

/// <summary>
/// Represents a pipeline application that can be run.
/// </summary>
public class PipelineHost : IDisposable
{
    private readonly ServiceProvider _services;

    internal PipelineHost(ServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _services.Dispose();
    }

    /// <summary>
    /// Runs the requested job of whichever registered pipeline the resolved project declares,
    /// returning its exit code.
    /// </summary>
    /// <param name="pipelines">The pipelines the host knows about.</param>
    /// <param name="runtimes">The runtimes the host can find itself running in.</param>
    /// <param name="args">What the command line asked for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<int> RunJob(PipelineRegistry pipelines, RuntimeRegistry runtimes, RunJobArgs args, CancellationToken cancellationToken = default)
    {
        // The whole registered model is judged before anything is loaded, so a malformed
        // pipeline or runtime fails every run at startup, not just the run that selects it.
        List<Error> model = [.. pipelines.Validate(), .. runtimes.Validate()];
        if (model.Count > 0)
        {
            return ConfigurationError(EngineConsole(args), model);
        }

        var runtime = runtimes.Detect();
        if (runtime.IsError)
        {
            return ConfigurationError(EngineConsole(args), runtime.Errors);
        }

        // The runtime speaks from here on: it knows whether debug logging was asked for, and how
        // this environment wants the narrative rendered.
        var console = runtime.Value.CreateConsole(args.LogLevel);

        var project = await RittenProject.Resolve(Environment.CurrentDirectory);
        if (project.IsError)
        {
            return ConfigurationError(console, project.Errors);
        }

        var knownPipelines = Result.Error($"Known pipelines: {string.Join(", ", pipelines.Names)}.");
        var name = project.Value.GetPipelineName();
        if (name.IsError)
        {
            return ConfigurationError(console, [.. name.Errors, knownPipelines]);
        }

        if (pipelines.Find(name.Value) is not { } pipeline)
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

        var builder = new PipelineHostBuilder(project.Value, pipeline.Label, console, runtime.Value, args.DryRun, args.AutoApprove);
        var host = builder.Build(declared);
        if (host.IsError)
        {
            return ConfigurationError(console, host.Errors);
        }

        using var _ = host.Value;
        return await host.Value.Run(cancellationToken);
    }

    // Before a runtime is selected there's nothing to ask for a console, so errors that early
    // print through the engine's own renderer.
    private static SpectrePipelineConsole EngineConsole(RunJobArgs args) => new(AnsiConsole.Console, args.LogLevel);

    private static int ConfigurationError(IPipelineLog log, IEnumerable<Error> errors)
    {
        log.Errors(errors);
        return PipelineExitCodes.ConfigurationError;
    }

    internal async Task<int> Run(CancellationToken cancellationToken)
    {
        var runner = _services.GetRequiredService<IPipelineRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }
}
