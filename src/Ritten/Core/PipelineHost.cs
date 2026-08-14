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
    /// Creates, configures, and runs the specified pipeline, returning its exit code.
    /// </summary>
    /// <typeparam name="TPipeline">The pipeline type to run.</typeparam>
    /// <typeparam name="TSettings">The settings taken by the pipeline.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<int> Run<TPipeline, TSettings>(CancellationToken cancellationToken = default) where TPipeline : Pipeline<TSettings>, new()
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
            return ConfigurationError(reporter, $"Could not read {RittenProject.FileName}: {exception.Message}", exception);
        }

        if (project is null)
        {
            return ConfigurationError(reporter, $"No {RittenProject.FileName} found in '{Environment.CurrentDirectory}' or any parent directory.");
        }

        TSettings settings;
        try
        {
            settings = project.GetSettings<TSettings>();
        }
        catch (JsonException exception)
        {
            return ConfigurationError(reporter, $"Could not read '{project.FilePath}': {exception.Message}", exception);
        }

        // Requirements are the pipeline's, not the file's: two pipelines can share a settings type
        // and still disagree about what has to be filled in.
        if (!pipeline.TryValidate(settings, out var failures))
        {
            foreach (var failure in failures)
            {
                reporter.Error(failure);
            }

            return PipelineExitCodes.ConfigurationError;
        }

        var builder = new PipelineHostBuilder(project, reporter);
        pipeline.Configure(builder, settings);
        builder.Services.AddSingleton<Pipeline>(pipeline);

        using var host = builder.Build();
        return await host.Run(cancellationToken);
    }

    internal async Task<int> Run(CancellationToken cancellationToken)
    {
        var runner = _services.GetRequiredService<IPipelineRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }

    /// <summary>
    /// A malformed or mistyped project file is the author's mistake, not a crash.
    /// </summary>
    private static int ConfigurationError(IPipelineLog log, string message, Exception? exception = null)
    {
        log.Error(message);
        if (exception is not null)
        {
            log.Log(PipelineLogLevel.Verbose, null, exception);
        }

        return PipelineExitCodes.ConfigurationError;
    }
}
