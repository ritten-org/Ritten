namespace Ritten.Contracts;

/// <summary>
/// Writes pipeline output to the terminal.
/// </summary>
public interface IPipelineLog
{
    /// <summary>
    /// Determines whether log entries at the given level should print to the terminal.
    /// </summary>
    bool IsEnabled(PipelineLogLevel level);

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="level">The kind of message being written.</param>
    /// <param name="message">The message content of the log.</param>
    /// <param name="exception">An optional exception behind the message.</param>
    void Log(PipelineLogLevel level, string? message, Exception? exception = null);
}
