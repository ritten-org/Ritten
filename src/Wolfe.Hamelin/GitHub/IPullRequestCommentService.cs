namespace Wolfe.Hamelin.GitHub;

/// <summary>
/// Maintains a single, updatable comment on the pull request that triggered the current run.
/// </summary>
public interface IPullRequestCommentService
{
    /// <summary>
    /// Creates the pipeline's comment on the current pull request, or updates it in place.
    /// </summary>
    Task CreateOrUpdate(string body, CancellationToken cancellationToken = default);
}
