using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.Reporting;

namespace Ritten.OpenTelemetry;

/// <summary>
/// Provides trace information for telemetry.
/// </summary>
internal sealed class TracerProviderLifetime : IWorkflowProgress, IDisposable
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(5);
    private static readonly string[] EndpointVariables = ["OTEL_EXPORTER_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"];

    private readonly TracerProvider? _provider;

    public TracerProviderLifetime(IServiceProvider services, WorkflowEnvironment environment)
    {
        if (EndpointVariables.Any(name => !string.IsNullOrWhiteSpace(environment.Get(name))))
        {
            _provider = services.GetService<TracerProvider>();
        }
    }

    public Task OnWorkflowStarted(WorkflowJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task OnStepStarted(Step step, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task OnWorkflowCompleted(WorkflowResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;

    // The container disposes the provider after this, having built it first; shutting it down here bounds the wait.
    public void Dispose() => _provider?.Shutdown((int)FlushTimeout.TotalMilliseconds);
}
