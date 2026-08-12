using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace Wolfe.Hamelin.GitHub;

internal class CommentService(ILogger<CommentService> logger, IOptions<GitHubOptions> options, IGitHubClient client) : ICommentService
{
    public async Task CreateOrUpdate(string body, CancellationToken cancellationToken = default)
    {
        if (options.Value.RepositoryId is not { } repositoryId)
        {
            logger.LogInformation("Repository ID not available; skipping pull request comment.");
            return;
        }

        if (!options.Value.IsPullRequest)
        {
            throw new InvalidOperationException("Attempted to create or update a pull request comment outside of a pull request context.");
        }

        var pullRequestNumber = options.Value.PullRequestNumber.Value;
        var fullBody = $"{Marker}\n{body}";

        var comments = await client.Issue.Comment.GetAllForIssue(repositoryId, pullRequestNumber);
        var existing = comments.FirstOrDefault(c => c.Body.StartsWith(Marker, StringComparison.Ordinal));
        if (existing == null)
        {
            logger.LogInformation("Creating comment on PR #{PullRequestNumber}.", pullRequestNumber);
            await client.Issue.Comment.Create(repositoryId, pullRequestNumber, fullBody);
        }
        else
        {
            logger.LogInformation("Updating existing comment on PR #{PullRequestNumber}.", pullRequestNumber);
            await client.Issue.Comment.Update(repositoryId, existing.Id, fullBody);
        }
    }

    // One comment per workflow, found again on later runs by this invisible prefix.
    private string Marker => $"<!-- hamelin-build:{Slug(options.Value.WorkflowName)} -->";

    private static string Slug(string value) =>
        string.Concat(value.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-'));
}
