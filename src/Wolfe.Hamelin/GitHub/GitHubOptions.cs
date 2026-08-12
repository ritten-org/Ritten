using System.Diagnostics.CodeAnalysis;

namespace Wolfe.Hamelin.GitHub;

/// <summary>
/// GitHub context for publishing the build report.
/// </summary>
public class GitHubOptions
{
    /// <summary>
    /// The token used to authenticate with the GitHub API.
    /// </summary>
    public string? Token { get; set; } = Environment.GetEnvironmentVariable("GH_TOKEN") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    /// <summary>
    /// The ID of the repository the pipeline is running against.
    /// </summary>
    public long? RepositoryId { get; set; } = ParseRepositoryId(Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_ID"));

    /// <summary>
    /// The number of the pull request that triggered the run, if there is one.
    /// </summary>
    public int? PullRequestNumber { get; set; } = ParsePullRequestNumber(Environment.GetEnvironmentVariable("GITHUB_REF"));

    /// <summary>
    /// The name of the workflow the pipeline is running in, used to title the build report.
    /// </summary>
    public string WorkflowName { get; set; } = Environment.GetEnvironmentVariable("GITHUB_WORKFLOW") ?? "Pipeline";

    /// <summary>
    /// True if the run was triggered by a pull request, otherwise false.
    /// </summary>
    [MemberNotNullWhen(true, nameof(PullRequestNumber))]
    public bool IsPullRequest => PullRequestNumber != null;

    private static long? ParseRepositoryId(string? value) =>
        long.TryParse(value, out var repositoryId) ? repositoryId : null;

    private static int? ParsePullRequestNumber(string? githubRef)
    {
        // Pull request runs check out a ref of the form `refs/pull/<number>/merge`.
        if (githubRef?.StartsWith("refs/pull/") != true)
        {
            return null;
        }

        return int.TryParse(githubRef.Split('/')[2], out var pullRequestNumber) ? pullRequestNumber : null;
    }
}
