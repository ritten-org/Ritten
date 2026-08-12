namespace Wolfe.Hamelin.Reporting;

/// <summary>
/// The tone of a report entry, ordered by severity.
/// </summary>
public enum ReportTone
{
    /// <summary>
    /// Neutral information.
    /// </summary>
    Note,

    /// <summary>
    /// Something completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Something needs attention but didn't fail the run.
    /// </summary>
    Warning,

    /// <summary>
    /// Something failed the run.
    /// </summary>
    Failure
}
