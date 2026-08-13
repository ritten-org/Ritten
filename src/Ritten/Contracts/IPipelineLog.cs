namespace Ritten.Contracts;

/// <summary>
/// Writes pipeline output to the terminal at three verbosity levels:
/// <see cref="Status"/> (always visible), <see cref="Detail"/> (hidden in quiet mode),
/// and <see cref="Verbose"/> (only shown with --verbose).
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

    /// <summary>
    /// Writes a diagnostic message that is only shown with --verbose.
    /// </summary>
    void Verbose(string message);
}
