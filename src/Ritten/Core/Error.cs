namespace Ritten.Core;

/// <summary>
/// One reason an operation failed, phrased for whoever has to fix it.
/// </summary>
/// <param name="Message">A message describing what was wrong.</param>
public record Error(string Message)
{
    /// <summary>
    /// Converts a message to an <see cref="Error"/>, so that callers can collect plain strings.
    /// </summary>
    /// <param name="message">A message describing what was wrong.</param>
    public static implicit operator Error(string message) => new(message);
}
