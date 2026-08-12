namespace Wolfe.Hamelin.Build.Reporting;

/// <summary>
/// The tone of a report entry, ordered by severity so that a section's overall tone is the maximum of its entries.
/// </summary>
public enum ReportTone
{
    Note,
    Success,
    Warning,
    Failure
}
