namespace Ritten.Engine;

/// <summary>
/// One reason an operation failed, phrased for whoever has to fix it.
/// </summary>
/// <param name="Message">A message describing what was wrong.</param>
/// <param name="Cause">The exception behind the failure, when there was one.</param>
public record Error(string Message, Exception? Cause = null)
{
    /// <summary>
    /// Content the reader is meant to copy.
    /// </summary>
    public string? Verbatim { get; init; }

    /// <summary>
    /// Converts a message to an <see cref="Error"/>, so that callers can collect plain strings.
    /// </summary>
    /// <param name="message">A message describing what was wrong.</param>
    public static implicit operator Error(string message) => new(message);

    /// <summary>
    /// Creates a new error with the given message.
    /// </summary>
    public static Error From(string message) => new(message);
}
