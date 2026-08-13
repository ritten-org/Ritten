namespace Ritten.Runtimes.GitHubActions;

/// <summary>
/// Provides information about the GitHub Actions runtime environment.
/// </summary>
internal interface IGitHubActionsRuntime
{
    /// <summary>
    /// Gets a value indicating whether the pipeline is running in a GitHub Actions environment.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the path to the pipeline summary file, if available.
    /// </summary>
    string? SummaryFile { get; }
}
