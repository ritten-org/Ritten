namespace Wolfe.Hamelin.Reporting;

/// <summary>
/// A single authored entry within a report section.
/// </summary>
public abstract record ReportEntry(ReportTone Tone)
{
    /// <summary>
    /// Renders the entry as GitHub-flavored Markdown.
    /// </summary>
    public abstract string ToMarkdown();
}
