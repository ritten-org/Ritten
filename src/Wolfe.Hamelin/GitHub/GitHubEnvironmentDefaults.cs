namespace Wolfe.Hamelin.GitHub;

/// <summary>
/// Populates <see cref="GitHubOptions"/> from the environment variables GitHub Actions provides.
/// </summary>
internal static class GitHubEnvironmentDefaults
{
    public static void Apply(GitHubOptions options, Func<string, string?> environment)
    {
        options.Token = environment("GH_TOKEN") ?? environment("GITHUB_TOKEN") ?? options.Token;
        options.RepositoryId = ParseRepositoryId(environment("GITHUB_REPOSITORY_ID")) ?? options.RepositoryId;
        options.PullRequestNumber = ParsePullRequestNumber(environment("GITHUB_REF")) ?? options.PullRequestNumber;
        options.WorkflowName = environment("GITHUB_WORKFLOW") ?? "Pipeline";
    }

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
