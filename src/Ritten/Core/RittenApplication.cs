using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ritten.Core.Runner;

namespace Ritten.Core;

/// <summary>
/// Represents a pipeline application that can be run.
/// </summary>
public class RittenApplication : IDisposable
{
    private readonly IHost _host;

    internal RittenApplication(IHost host)
    {
        _host = host;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _host.Dispose();
    }

    /// <summary>
    /// Creates, configures, and runs the specified pipeline, returning its exit code.
    /// </summary>
    /// <typeparam name="TPipeline">The pipeline type to run.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<int> Run<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : Pipeline, new()
    {
        var builder = new RittenApplicationBuilder(new RittenApplicationOptions());
        new TPipeline().Configure(builder);

        using var app = builder.Build();
        var runner = app._host.Services.GetRequiredService<IPipelineRunner>();
        var summary = await runner.RunPipeline(cancellationToken);
        return summary.ExitCode;
    }
}
