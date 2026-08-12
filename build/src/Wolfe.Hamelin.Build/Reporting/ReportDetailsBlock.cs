namespace Wolfe.Hamelin.Build.Reporting;

/// <summary>
/// A collapsible block for content too long to render inline, such as test failures or compiler output.
/// </summary>
public sealed record ReportDetailsBlock(ReportTone Tone, string Summary, string Markdown) : ReportEntry(Tone)
{
    public override string ToMarkdown() =>
        $"""
         <details>
         <summary>{Summary}</summary>

         {Markdown.Trim()}

         </details>
         """;
}
