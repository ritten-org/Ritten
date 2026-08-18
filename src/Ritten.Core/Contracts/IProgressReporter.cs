using Ritten.Engine;

namespace Ritten.Contracts;

/// <summary>
/// Receives lifecycle notifications from the workflow runner.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Called when the job is about to start.
    /// </summary>
    Task OnWorkflowStarted(WorkflowJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a step is about to run.
    /// </summary>
    Task OnStepStarted(Step step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a step has finished executing.
    /// </summary>
    Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the workflow has finished executing.
    /// </summary>
    Task OnWorkflowCompleted(WorkflowResult result, CancellationToken cancellationToken = default);
}
