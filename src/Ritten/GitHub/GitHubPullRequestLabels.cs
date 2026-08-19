using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Contracts;
using Label = Ritten.Contracts.Label;

namespace Ritten.GitHub;

/// <summary>
/// Reads the pull request's labels from the GitHub API.
/// </summary>
/// <param name="client">The GitHub client the labels are read with.</param>
/// <param name="options">The Actions context naming the repository and pull request.</param>
public sealed class GitHubPullRequestLabels(IGitHubClient client, IOptions<GitHubActionsOptions> options) : IPullRequestLabels
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Label>?> Get(CancellationToken cancellationToken = default)
    {
        var actions = options.Value;
        if (actions.RepositoryId is null || !actions.IsPullRequest)
        {
            return null;
        }

        var labels = await client.Issue.Labels.GetAllForIssue(actions.RepositoryId.Value, actions.PullRequestNumber.Value);
        return [.. labels.Select(label => new Label(label.Name) { Color = label.Color, Description = label.Description })];
    }
}
