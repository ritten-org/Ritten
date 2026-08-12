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
    public string? Token { get; set; }

    /// <summary>
    /// The ID of the repository the pipeline is running against.
    /// </summary>
    public long? RepositoryId { get; set; }

    /// <summary>
    /// The number of the pull request that triggered the run, if there is one.
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// The name of the workflow the pipeline is running in, used to title the build report.
    /// </summary>
    public string WorkflowName { get; set; } = "Pipeline";

    /// <summary>
    /// True if the run was triggered by a pull request, otherwise false.
    /// </summary>
    [MemberNotNullWhen(true, nameof(PullRequestNumber))]
    public bool IsPullRequest => PullRequestNumber != null;
}
