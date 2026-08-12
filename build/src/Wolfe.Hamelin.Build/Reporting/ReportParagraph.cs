namespace Wolfe.Hamelin.Build.Reporting;

/// <summary>
/// A markdown paragraph (or bullet list, code block, etc.) rendered inline in the section.
/// </summary>
public sealed record ReportParagraph(ReportTone Tone, string Markdown) : ReportEntry(Tone)
{
    public override string ToMarkdown() => Markdown.Trim();
}
