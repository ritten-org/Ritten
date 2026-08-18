using Microsoft.Extensions.DependencyInjection;
using Ritten.Core.Runner;

namespace Ritten.Core;

/// <summary>
/// An assembled run, ready to execute.
/// </summary>
public sealed class PipelineRun : IDisposable
{
    private readonly ServiceProvider _services;

    internal PipelineRun(ServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _services.Dispose();
    }

    internal async Task<int> Run(CancellationToken cancellationToken)
    {
        var runner = _services.GetRequiredService<IPipelineRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }
}
