namespace Ritten.Contracts;

/// <summary>
/// Writes pipeline output to the terminal.
/// </summary>
public interface IPipelineLog
{
    /// <summary>
    /// Writes a message at the given level.
    /// </summary>
    /// <param name="level">The kind of message being written.</param>
    /// <param name="message">The message.</param>
    void Log(PipelineLogLevel level, string message);
}
