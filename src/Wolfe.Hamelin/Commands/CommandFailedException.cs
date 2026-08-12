namespace Wolfe.Hamelin.Commands;

/// <summary>
/// Thrown when a command built with <see cref="Command.ThrowOnError"/> exits non-zero.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="result">The result of the failed command.</param>
public sealed class CommandFailedException(string message, CommandResult result) : Exception(message)
{
    /// <summary>
    /// The result of the failed command.
    /// </summary>
    public CommandResult Result { get; } = result;
}
