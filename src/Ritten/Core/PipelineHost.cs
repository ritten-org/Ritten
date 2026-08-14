using Microsoft.Extensions.Configuration;
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
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<int> Run<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : Pipeline, new()
    {
        var reporter = new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail);

        if (RittenProject.Find(Environment.CurrentDirectory) is not { } rootPath)
        {
            reporter.Error($"No {RittenProject.FileName} found in '{Environment.CurrentDirectory}' or any parent directory.");
            return PipelineExitCodes.ConfigurationError;
        }

        IConfiguration configuration;
        try
        {
            configuration = RittenProject.ReadConfiguration(rootPath);
        }
        catch (Exception exception)
        {
            reporter.Error($"Could not read '{Path.Combine(rootPath, RittenProject.FileName)}'.", exception);
            return PipelineExitCodes.ConfigurationError;
        }

        var builder = new PipelineHostBuilder(rootPath, configuration, reporter);
        var pipeline = new TPipeline();
        pipeline.Configure(builder);
        builder.Services.AddSingleton<Pipeline>(pipeline);

        using var host = builder.Build();
        return await host.Run(cancellationToken);
    }

    internal async Task<int> Run(CancellationToken cancellationToken)
    {
        var log = _services.GetRequiredService<IPipelineLog>();

        if (!ConfigurationValidator.TryValidate(_services, out var failures))
        {
            foreach (var failure in failures)
            {
                log.Error(failure);
            }

            return PipelineExitCodes.ConfigurationError;
        }

        var runner = _services.GetRequiredService<IPipelineRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }
}
