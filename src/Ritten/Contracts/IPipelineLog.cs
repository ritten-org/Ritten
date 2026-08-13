namespace Ritten.Contracts;

/// <summary>
/// Writes pipeline output to the terminal. Use <see cref="Status"/> for key progress messages
/// and <see cref="Detail"/> for supplementary information that can be suppressed in quiet mode.
/// </summary>
public interface IPipelineLog
{
    /// <summary>
    /// Writes a progress message that is always visible.
    /// </summary>
    void Status(string message);

    /// <summary>
    /// Writes a detail message that is hidden in quiet mode.
    /// </summary>
    void Detail(string message);
}
