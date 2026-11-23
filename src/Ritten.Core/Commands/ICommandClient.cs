namespace Ritten.Commands;

/// <summary>
/// Provides a contract for a client of a command-line tool.
/// </summary>
public interface ICommandClient
{
    /// <summary>
    /// Runs the command created by <paramref name="configure"/>.
    /// </summary>
    /// <param name="configure">The command, made of the client's.</param>
    /// <param name="ct">Cancels the command.</param>
    Task<CommandResult> RunCommand(Func<Command, Command> configure, CancellationToken ct = default);
}
