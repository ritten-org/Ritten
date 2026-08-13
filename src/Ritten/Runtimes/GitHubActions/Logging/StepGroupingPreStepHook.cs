using Ritten.Contracts.Hooks;
using Ritten.Contracts.Runtime;

namespace Ritten.Runtimes.GitHubActions.Logging;

internal class StepGroupingPreStepHook(IRuntimeCommands commands) : IPreStepHook
{
    public Task PreStep(PreStepHookArgs args, CancellationToken cancellationToken = default)
    {
        commands.BeginGroup(args.StepName);
        return Task.CompletedTask;
    }
}
