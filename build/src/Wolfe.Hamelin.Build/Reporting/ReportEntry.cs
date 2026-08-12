namespace Wolfe.Hamelin.Build.Reporting;

/// <summary>
/// A single authored entry within a report section.
/// </summary>
public abstract record ReportEntry(ReportTone Tone)
{
    public abstract string ToMarkdown();
}
