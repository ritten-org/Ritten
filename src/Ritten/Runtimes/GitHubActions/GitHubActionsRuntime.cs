namespace Ritten.Runtimes.GitHubActions;

internal class GitHubActionsRuntime
{
    /// <summary>
    /// True if the pipeline is running in a GitHub Actions environment.
    /// </summary>
    public static bool IsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    /// <summary>
    /// Gets the path to the pipeline summary file.
    /// </summary>
    public static string? SummaryFile => Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
}
