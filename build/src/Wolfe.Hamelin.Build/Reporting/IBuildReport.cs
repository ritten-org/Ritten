namespace Wolfe.Hamelin.Build.Reporting;

/// <summary>
/// Accumulates authored feedback from pipeline steps for publication at the end of the run.
/// </summary>
public interface IBuildReport
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
