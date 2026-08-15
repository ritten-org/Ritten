namespace Ritten.Runtimes.GitHubActions;

/// <summary>
/// The environment variables GitHub Actions defines.
/// </summary>
internal static class GitHubEnvironment
{
    /// <summary>
    /// The token used to authenticate with the GitHub API.
    /// </summary>
    public const string Token = "GH_TOKEN";

    /// <summary>
    /// The token GitHub Actions provides to a workflow by default.
    /// </summary>
    public const string DefaultToken = "GITHUB_TOKEN";

    /// <summary>
    /// The ID of the repository the workflow is running against.
    /// </summary>
    public const string RepositoryId = "GITHUB_REPOSITORY_ID";

    /// <summary>
    /// The ref being built; pull request runs use <c>refs/pull/&lt;number&gt;/merge</c>.
    /// </summary>
    public const string Ref = "GITHUB_REF";

    /// <summary>
    /// Set whenever a workflow is running in GitHub Actions.
    /// </summary>
    public const string Actions = "GITHUB_ACTIONS";

    /// <summary>
    /// The file the job summary is appended to.
    /// </summary>
    public const string StepSummary = "GITHUB_STEP_SUMMARY";

    /// <summary>
    /// The name of the workflow being run.
    /// </summary>
    public const string Workflow = "GITHUB_WORKFLOW";

    /// <summary>
    /// Set to <c>1</c> when step debug logging is on.
    /// </summary>
    public const string RunnerDebug = "RUNNER_DEBUG";

    /// <summary>
    /// Whether the run asked for debug logging, as "Re-run with debug logging" does.
    /// </summary>
    public static bool IsDebug() => IsDebug(Environment.GetEnvironmentVariable);

    /// <summary>
    /// Whether the given environment asked for debug logging.
    /// </summary>
    internal static bool IsDebug(Func<string, string?> envVar) => envVar(RunnerDebug) == "1";
}
