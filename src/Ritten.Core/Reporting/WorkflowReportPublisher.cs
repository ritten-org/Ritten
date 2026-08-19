using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.Reporting.Sinks;

namespace Ritten.Reporting;

/// <summary>
/// Publishes the final build report to every registered sink when the workflow finishes.
/// </summary>
internal class WorkflowReportPublisher(
    IWorkflowLog log,
    RunContext context,
    IWorkflowReport report,
    IEnumerable<IWorkflowResultSink> sinks
) : IWorkflowProgress
{
    /// <inheritdoc />
    public async Task OnWorkflowStarted(WorkflowJob job, CancellationToken cancellationToken)
    {
        foreach (var sink in sinks)
        {
            try
            {
                await sink.Started(job, cancellationToken);
            }
            catch (Exception ex)
            {
                log.Warning($"Failed to announce the run via {sink.GetType().Name}", ex);
            }
        }
    }

    /// <inheritdoc />
    public Task OnStepStarted(Step step, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task OnWorkflowCompleted(WorkflowResult result, CancellationToken cancellationToken)
    {
        var finished = new WorkflowReport(context.Title, result.IsSuccess, report.Sections, result.FailedStep);
        foreach (var sink in sinks)
        {
            try
            {
                await sink.Publish(finished, cancellationToken);
            }
            catch (Exception ex)
            {
                log.Warning($"Failed to publish the build report via {sink.GetType().Name}", ex);
            }
        }
    }
}
