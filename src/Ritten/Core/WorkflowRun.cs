using Microsoft.Extensions.DependencyInjection;
using Ritten.Core.Runner;

namespace Ritten.Core;

/// <summary>
/// An assembled run, ready to execute.
/// </summary>
public sealed class WorkflowRun : IDisposable
{
    private readonly ServiceProvider _services;

    internal WorkflowRun(ServiceProvider services)
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
        var runner = _services.GetRequiredService<IWorkflowRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }
}
