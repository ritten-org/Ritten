namespace Ritten.Commands;

/// <summary>
/// Provides an abstraction about running command line applications and wrangling the output.
/// </summary>
public interface ICommandRunner
{
    /// <summary>
    /// Runs the given command and returns the result.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The result of the command.</returns>
    /// <remarks>
    /// A non-zero exit doesn't throw unless the command was built with <see cref="Command.ThrowOnError"/>;
    /// otherwise check <see cref="CommandResult.IsSuccess"/> on the result.
    /// </remarks>
    Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default);
}
