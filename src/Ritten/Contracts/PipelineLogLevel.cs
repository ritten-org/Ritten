namespace Ritten.Contracts;

/// <summary>
/// The kind of message being written to an <see cref="IPipelineLog"/>.
/// </summary>
public enum PipelineLogLevel
{
    /// <summary>
    /// Diagnostic output, only shown with --verbose.
    /// </summary>
    Verbose,

    /// <summary>
    /// Supporting detail about what a step is doing, hidden with --quiet.
    /// </summary>
    Detail,

    /// <summary>
    /// Progress through the pipeline. Always visible.
    /// </summary>
    Status,

    /// <summary>
    /// Something went wrong without failing the pipeline. Always visible.
    /// </summary>
    Warning,

    /// <summary>
    /// Something failed outside of a step. Always visible.
    /// </summary>
    Error
}
