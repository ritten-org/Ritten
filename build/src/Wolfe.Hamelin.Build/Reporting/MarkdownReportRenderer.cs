namespace Wolfe.Hamelin.Build.Reporting;

public class MarkdownReportRenderer
{
    /// <summary>
    /// Renders the report as GitHub-flavoured markdown.
    /// </summary>
    public string Render(string title, bool succeeded, IReadOnlyList<ReportSection> sections)
    {
        var blocks = new List<string> { $"## {(succeeded ? "✅" : "❌")} {title}" };

        foreach (var section in sections.Where(s => s.Entries.Count > 0))
        {
            blocks.Add($"### {section.Tone.Icon()} {section.Title}");
            blocks.AddRange(section.Entries.Select(e => e.ToMarkdown()));
        }

        if (!succeeded && sections.All(s => s.Tone != ReportTone.Failure))
        {
            blocks.Add("The run failed for a reason that isn't reported here — check the build logs for details.");
        }

        return string.Join("\n\n", blocks) + "\n";
    }
}
