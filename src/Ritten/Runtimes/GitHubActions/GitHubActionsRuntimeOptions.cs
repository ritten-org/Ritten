namespace Ritten.Runtimes.GitHubActions;

internal class GitHubActionsRuntimeOptions
{
    public bool EnableLogFormatter { get; set; } = true;

    public bool EnableLogGrouping { get; set; } = true;

    public Func<bool> RuntimeDetector { get; set; } = () => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
}
