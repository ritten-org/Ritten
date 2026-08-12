namespace Wolfe.Hamelin.Build.Reporting.GitHub;

public interface IPullRequestCommentService
{
    /// <summary>
    /// Creates the pipeline's comment on the current pull request, or updates it in place.
    /// </summary>
    Task CreateOrUpdate(string body, CancellationToken cancellationToken = default);
}
