using Ritten.Contracts;

namespace Ritten.Reporting.Sinks;

/// <summary>
/// A destination the finished run's report is published to.
/// </summary>
public interface IWorkflowResultSink
{
    /// <summary>
    /// Announces that the run is underway.
    /// </summary>
    /// <param name="job">The job the run is executing.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Started(WorkflowJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Publishes the finished run's report.
    /// </summary>
    /// <param name="report">The report to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Publish(Report report, CancellationToken cancellationToken = default);
}
