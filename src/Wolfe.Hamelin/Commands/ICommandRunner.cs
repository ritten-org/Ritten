namespace Wolfe.Hamelin.Commands;

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
    /// This method doesn't throw if the command exits non-zero, so check the result or call <see cref="CommandResult.ThrowOnError"/>.
    /// </remarks>
    Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default);
}
