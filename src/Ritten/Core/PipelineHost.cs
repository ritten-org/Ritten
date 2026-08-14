using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runner;
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
    /// Runs one job of the specified pipeline, returning its exit code.
    /// </summary>
    /// <typeparam name="TPipeline">The pipeline the job belongs to.</typeparam>
    /// <typeparam name="TSettings">The settings taken by the pipeline.</typeparam>
    /// <param name="job">The job to run.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<int> Run<TPipeline, TSettings>(string job, CancellationToken cancellationToken = default)
        where TPipeline : Pipeline<TSettings>, new()
        where TSettings : class
    {
        var reporter = new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail);
        var pipeline = new TPipeline();

        var project = await RittenProject.Resolve(Environment.CurrentDirectory);
        if (project.IsError)
        {
            return ConfigurationError(reporter, project.Errors);
        }

        var settings = project.Value.GetSettings<TSettings>();
        if (settings.IsError)
        {
            return ConfigurationError(reporter, settings.Errors);
        }

        var builder = new PipelineHostBuilder(project.Value, pipeline.Name, reporter);
        pipeline.Configure(builder, settings.Value);

        var host = builder.Build(job);
        if (host.IsError)
        {
            return ConfigurationError(reporter, host.Errors);
        }

        using var _ = host.Value;
        return await host.Value.Run(cancellationToken);
    }

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
