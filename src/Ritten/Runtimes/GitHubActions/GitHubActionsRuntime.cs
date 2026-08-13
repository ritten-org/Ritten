namespace Ritten.Runtimes.GitHubActions;

/// <inheritdoc />
internal class GitHubActionsRuntime : IGitHubActionsRuntime
{
    /// <inheritdoc />
    public bool IsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    /// <inheritdoc />
    public string? SummaryFile => Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
}
