using System.Diagnostics.CodeAnalysis;

namespace Wolfe.Hamelin.Build.Reporting.GitHub;

/// <summary>
/// GitHub context for publishing the build report. Defaults come from the environment
/// variables GitHub Actions provides; everything is overridable via the `GitHub` config
/// section (e.g. `GitHub__Token`).
/// </summary>
public class GitHubOptions
{
    public string? Token { get; set; } = Environment.GetEnvironmentVariable("GH_TOKEN") ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    public long? RepositoryId { get; set; } = ParseRepositoryId(Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_ID"));

    public int? PullRequestNumber { get; set; } = ParsePullRequestNumber(Environment.GetEnvironmentVariable("GITHUB_REF"));

    public string WorkflowName { get; set; } = Environment.GetEnvironmentVariable("GITHUB_WORKFLOW") ?? "Pipeline";

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
