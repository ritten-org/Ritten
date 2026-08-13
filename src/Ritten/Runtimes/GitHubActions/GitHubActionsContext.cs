using Ritten.Contracts.Runtime;

namespace Ritten.Runtimes.GitHubActions;

internal class GitHubActionsContext : IRuntimeContext
{
    public bool IsCI { get; init; }
}
