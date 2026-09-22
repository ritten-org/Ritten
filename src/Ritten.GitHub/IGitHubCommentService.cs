namespace Ritten.GitHub;

/// <summary>
/// Maintains a single, updatable comment on the pull request that triggered the current run.
/// </summary>
public interface IGitHubCommentService
{
    /// <summary>
    /// Creates the workflow's comment on the current pull request, or updates it in place.
    /// </summary>
    Task CreateOrUpdate(string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes this workflow's comment from the pull request, if there is one.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task Delete(CancellationToken cancellationToken = default);
}
