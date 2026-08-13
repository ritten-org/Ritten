using Ritten.Core;

namespace Ritten.Contracts;

/// <summary>
/// Receives lifecycle notifications from the pipeline runner.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Called when the pipeline is about to start.
    /// </summary>
    Task OnPipelineStarted(Pipeline pipeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a step is about to run.
    /// </summary>
    Task OnStepStarted(IPipelineStep step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a step has finished executing.
    /// </summary>
    Task OnStepCompleted(IPipelineStep step, StepResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the pipeline has finished executing.
    /// </summary>
    Task OnPipelineCompleted(PipelineResult result, CancellationToken cancellationToken = default);
}
