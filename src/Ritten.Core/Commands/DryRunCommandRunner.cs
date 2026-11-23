using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Commands;

/// <summary>
/// Guards against running commands that would make changes to the environment.
/// </summary>
internal sealed class DryRunCommandRunner(IWorkflowLog log, ICommandRunner inner) : ICommandRunner
{
    /// <inheritdoc />
    public Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default)
    {
        if (command.Effect == CommandEffect.None)
        {
            return inner.Run(command, cancellationToken);
        }

        log.Skipped(Described(command));
        return Task.FromResult(new CommandResult(ExitCode.Success, string.Empty, string.Empty));
    }

    private static string Described(Command command) => command switch
    {
        { Description: { } description } => $"Would {description}.",
        { ArgumentsRedacted: true } => $"Would run {command.Path}.",
        _ => $"Would run `{string.Join(" ", [command.Path, .. command.Arguments])}`."
    };
}
