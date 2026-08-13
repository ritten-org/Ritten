namespace Ritten.Reporting.Sinks;

/// <summary>
/// A destination the rendered report is published to.
/// </summary>
public interface IReportSink
{
    /// <summary>
    /// Publishes the given report.
    /// </summary>
    Task Publish(string markdown, CancellationToken cancellationToken = default);
}
