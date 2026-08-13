namespace Ritten.Runtimes.GitHubActions;

internal class GitHubActionsRuntimeOptions
{
    public Func<bool> RuntimeDetector { get; set; } = () => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
}
