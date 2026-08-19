using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.GitHub;

internal class GitHubGitHubCommentService(
    IWorkflowLog log,
    IOptions<GitHubActionsOptions> options,
    RunContext context,
    IGitHubClient client
) : IGitHubCommentService
{
    public async Task CreateOrUpdate(string body, CancellationToken cancellationToken = default)
    {
        if (options.Value.RepositoryId is not { } repositoryId)
        {
            log.Detail("Repository ID not available; skipping pull request comment.");
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
            log.Detail($"Creating comment on PR #{pullRequestNumber}.");
            await client.Issue.Comment.Create(repositoryId, pullRequestNumber, fullBody);
        }
        else
        {
            log.Detail($"Updating existing comment on PR #{pullRequestNumber}.");
            await client.Issue.Comment.Update(repositoryId, existing.Id, fullBody);
        }
    }

    // One comment per workflow, found again on later runs by this invisible prefix.
    private string Marker => $"<!-- ritten:{Slug(context.Title)} -->";

    private static string Slug(string value) =>
        string.Concat(value.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-'));
}
