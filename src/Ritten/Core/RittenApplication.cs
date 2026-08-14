using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runner;

namespace Ritten.Core;

/// <summary>
/// Represents a pipeline application that can be run.
/// </summary>
public class RittenApplication : IDisposable
{
    private readonly ServiceProvider _services;

    internal RittenApplication(ServiceProvider services)
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
        var builder = new RittenApplicationBuilder();
        var pipeline = new TPipeline();
        pipeline.Configure(builder);
        builder.Services.AddSingleton<Pipeline>(pipeline);

        using var app = builder.Build();
        return await app.Run(cancellationToken);
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
