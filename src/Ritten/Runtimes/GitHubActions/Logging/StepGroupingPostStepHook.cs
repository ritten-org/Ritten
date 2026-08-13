using Ritten.Contracts.Hooks;
using Ritten.Contracts.Runtime;

namespace Ritten.Runtimes.GitHubActions.Logging;

internal class StepGroupingPostStepHook(IRuntimeCommands commands) : IPostStepHook
{
    public Task PostStep(PostStepHookArgs args, CancellationToken cancellationToken = default)
    {
        commands.EndGroup();
        return Task.CompletedTask;
    }
}
