namespace Ritten.Reporting;

/// <summary>
/// A destination whose published reports can be found again.
/// </summary>
public interface IPublishedReports
{
    /// <summary>
    /// The keys of the reports it holds.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<IReadOnlyList<ReportKey>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the report with the given key, if there is one.
    /// </summary>
    /// <param name="key">The report's key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Delete(ReportKey key, CancellationToken cancellationToken = default);
}
