using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Reporting;

namespace Ritten.GitHub;

internal class GitHubGitHubCommentService(
    IWorkflowLog log,
    IOptions<GitHubActionsOptions> options,
    IGitHubClient client
) : IGitHubCommentService
{
    private const string MarkerPrefix = "<!-- ritten:";
    private const string MarkerSuffix = " -->";

    public async Task CreateOrUpdate(ReportKey key, string body, CancellationToken cancellationToken = default)
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
        var fullBody = $"{MarkerOf(key)}\n{body}";

        var comments = await client.Issue.Comment.GetAllForIssue(repositoryId, pullRequestNumber);
        var existing = comments.FirstOrDefault(c => KeyOf(c.Body) == key);
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

    public async Task Delete(ReportKey key, CancellationToken cancellationToken = default)
    {
        if (options.Value.RepositoryId is not { } repositoryId || !options.Value.IsPullRequest)
        {
            return;
        }

        var pullRequestNumber = options.Value.PullRequestNumber.Value;
        var comments = await client.Issue.Comment.GetAllForIssue(repositoryId, pullRequestNumber);
        if (comments.FirstOrDefault(c => KeyOf(c.Body) == key) is not { } existing)
        {
            return;
        }

        log.Detail($"Removing the comment on PR #{pullRequestNumber}: nothing to say.");
        await client.Issue.Comment.Delete(repositoryId, existing.Id);
    }

    public async Task<IReadOnlyList<ReportKey>> List(CancellationToken cancellationToken = default)
    {
        if (options.Value.RepositoryId is not { } repositoryId || !options.Value.IsPullRequest)
        {
            return [];
        }

        var comments = await client.Issue.Comment.GetAllForIssue(repositoryId, options.Value.PullRequestNumber.Value);
        return [.. comments.Select(c => KeyOf(c.Body)).OfType<ReportKey>()];
    }

    /// <summary>
    /// The invisible prefix a report's comment is found again by on later runs.
    /// </summary>
    internal static string MarkerOf(ReportKey key) => $"{MarkerPrefix}{key}{MarkerSuffix}";

    private static ReportKey? KeyOf(string? body)
    {
        if (body is null || !body.StartsWith(MarkerPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var end = body.IndexOf(MarkerSuffix, MarkerPrefix.Length, StringComparison.Ordinal);
        return end < 0 ? null : ReportKey.Parse(body[MarkerPrefix.Length..end]);
    }
}
