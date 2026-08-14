using System.Text.Json;
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
    public static async Task<int> Run<TPipeline, TSettings>(string job, CancellationToken cancellationToken = default) where TPipeline : Pipeline<TSettings>, new()
    {
        var reporter = new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail);
        var pipeline = new TPipeline();

        RittenProject? project;
        try
        {
            project = await RittenProject.Resolve(Environment.CurrentDirectory);
        }
        catch (JsonException exception)
        {
            reporter.Error($"Could not read {RittenProject.FileName}: {exception.Message}", exception);
            return PipelineExitCodes.ConfigurationError;
        }

        if (project is null)
        {
            reporter.Error($"{Environment.CurrentDirectory} is not a valid Ritten project.");
            return PipelineExitCodes.ConfigurationError;
        }

        TSettings settings;
        try
        {
            settings = project.GetSettings<TSettings>();
        }
        catch (JsonException exception)
        {
            reporter.Error($"Could not read '{project.FilePath}'", exception);
            return PipelineExitCodes.ConfigurationError;
        }

        var builder = new PipelineHostBuilder(project, pipeline.Name, reporter);
        pipeline.Configure(builder, settings);

        var result = builder.Build(job);
        if (result.IsError)
        {
            reporter.Errors(result.Errors);
            return PipelineExitCodes.ConfigurationError;
        }

        using var host = result.Value;
        return await host.Run(cancellationToken);
    }

    internal async Task<int> Run(CancellationToken cancellationToken)
    {
        var runner = _services.GetRequiredService<IPipelineRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }
}
