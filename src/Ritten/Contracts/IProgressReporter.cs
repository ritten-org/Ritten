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
    Task OnPipelineStarted(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a step is about to run.
    /// </summary>
    /// <param name="stepName">The display name of the step.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task OnStepStarted(string stepName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a step has finished executing.
    /// </summary>
    /// <param name="result">The execution summary for the completed step.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task OnStepCompleted(StepResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the pipeline has finished executing.
    /// </summary>
    /// <param name="exitCode">The exit code of the pipeline.</param>
    /// <param name="result">The execution summaries of all steps that ran.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task OnPipelineCompleted(int exitCode, PipelineResult result, CancellationToken cancellationToken = default);
}
