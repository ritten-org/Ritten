using System.Diagnostics.CodeAnalysis;

namespace Ritten.GitHub;

/// <summary>
/// What the GitHub Actions runtime knows about the run that triggered it.
/// </summary>
public class GitHubActionsOptions
{
    /// <summary>
    /// The ID of the repository the workflow is running against.
    /// </summary>
    public long? RepositoryId { get; set; }

    /// <summary>
    /// The number of the pull request that triggered the run, if there is one.
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// The ref the pull request wants to merge into, if the run is one.
    /// </summary>
    public string? BaseRef { get; set; }

    /// <summary>
    /// The web page for the current workflow run, where the logs live.
    /// </summary>
    public string? RunUrl { get; set; }

    /// <summary>
    /// The path to the GitHub Actions job summary file, if available.
    /// </summary>
    public string? SummaryFile { get; set; }

    /// <summary>
    /// True if the run was triggered by a pull request, otherwise false.
    /// </summary>
    [MemberNotNullWhen(true, nameof(PullRequestNumber))]
    public bool IsPullRequest => PullRequestNumber != null;

    /// <summary>
    /// Configures the given options from the given environment.
    /// </summary>
    internal static void ConfigureFromEnvironment(GitHubActionsOptions options, Func<string, string?> envVar)
    {
        options.RepositoryId = ParseRepositoryId(envVar(GitHubEnvironment.RepositoryId));
        options.PullRequestNumber = ParsePullRequestNumber(envVar(GitHubEnvironment.Ref));
        options.BaseRef = envVar(GitHubEnvironment.BaseRef);
        options.SummaryFile = envVar(GitHubEnvironment.StepSummary);
        options.RunUrl = BuildRunUrl(envVar(GitHubEnvironment.ServerUrl), envVar(GitHubEnvironment.Repository), envVar(GitHubEnvironment.RunId));
    }

    private static string? BuildRunUrl(string? serverUrl, string? repository, string? runId) =>
        string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(runId)
            ? null
            : $"{serverUrl}/{repository}/actions/runs/{runId}";

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
