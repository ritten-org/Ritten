using Ritten.Reporting;

namespace Ritten.GitHub;

/// <summary>
/// Maintains updatable report comments on the pull request that triggered the current run, one per report key.
/// </summary>
public interface IGitHubCommentService : IPublishedReports
{
    /// <summary>
    /// Creates the report's comment on the current pull request, or updates it in place.
    /// </summary>
    /// <param name="key">The report's key.</param>
    /// <param name="body">The comment's markdown.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task CreateOrUpdate(ReportKey key, string body, CancellationToken cancellationToken = default);
}
