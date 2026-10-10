namespace Ritten.Reporting;

/// <summary>
/// The narrative outside a run, where nobody is watching one.
/// </summary>
internal sealed class SilentWorkflowLog : IWorkflowLog
{
    /// <inheritdoc />
    public bool IsEnabled(WorkflowLogLevel level) => false;

    /// <inheritdoc />
    public void Log(WorkflowLogLevel level, string? message, Exception? exception = null)
    {
    }
}
