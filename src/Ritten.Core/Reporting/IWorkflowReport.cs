namespace Ritten.Reporting;

/// <summary>
/// Accumulates authored feedback from workflow steps for publication at the end of the run.
/// </summary>
public interface IWorkflowReport
{
    /// <summary>
    /// The sections written so far, in the order they were first touched.
    /// </summary>
    IReadOnlyList<ReportSection> Sections { get; }

    /// <summary>
    /// Gets the section with the given title, creating it if it does not exist yet.
    /// </summary>
    ReportSection Section(string title);
}
