namespace Ritten.Reporting;

/// <summary>
/// Writes workflow output to the terminal.
/// </summary>
public interface IWorkflowLog
{
    /// <summary>
    /// Determines whether log entries at the given level should print to the terminal.
    /// </summary>
    bool IsEnabled(WorkflowLogLevel level);

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="level">The kind of message being written.</param>
    /// <param name="message">The message content of the log.</param>
    /// <param name="exception">An optional exception behind the message.</param>
    void Log(WorkflowLogLevel level, string? message, Exception? exception = null);
}
