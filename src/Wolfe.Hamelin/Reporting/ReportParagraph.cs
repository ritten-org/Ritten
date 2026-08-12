namespace Wolfe.Hamelin.Reporting;

/// <summary>
/// A markdown paragraph (or bullet list, code block, etc.) rendered inline in the section.
/// </summary>
public sealed record ReportParagraph(ReportTone Tone, string Markdown) : ReportEntry(Tone)
{
    /// <inheritdoc />
    public override string ToMarkdown() => Markdown.Trim();
}
