namespace Ritten.Reporting;

/// <summary>
/// The kind of message being written to an <see cref="IWorkflowLog"/>.
/// </summary>
public enum WorkflowLogLevel
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
    /// Progress through the workflow. Always visible.
    /// </summary>
    Status,

    /// <summary>
    /// An action deliberately not taken.
    /// </summary>
    Skipped,

    /// <summary>
    /// Something went wrong without failing the workflow. Always visible.
    /// </summary>
    Warning,

    /// <summary>
    /// Something failed outside of a step. Always visible.
    /// </summary>
    Error
}
