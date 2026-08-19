namespace Ritten.Reporting;

/// <summary>
/// Renders the report model as GitHub-flavored Markdown.
/// </summary>
public class MarkdownReportRenderer
{
    /// <summary>
    /// Renders the report as GitHub-flavored Markdown.
    /// </summary>
    /// <param name="report">The finished run's report.</param>
    public string Render(WorkflowReport report)
    {
        var blocks = new List<string> { $"## {(report.Succeeded ? "✅" : "❌")} {report.Title}" };

        foreach (var section in report.Sections.Where(s => s.Entries.Count > 0))
        {
            blocks.Add($"### {section.Tone.Icon()} {section.Title}");
            blocks.AddRange(section.Entries.Select(e => e.ToMarkdown()));
        }

        // A failing step that authored nothing still has its errors on the step result;
        // fall back to those before admitting the reader has to go digging in the logs.
        if (!report.Succeeded && report.Sections.All(s => s.Tone != ReportTone.Failure))
        {
            if (report.Failure?.Result.Errors is { Count: > 0 } errors)
            {
                blocks.Add($"### {ReportTone.Failure.Icon()} {report.Failure.Step.Name}");
                blocks.Add($"```\n{string.Join('\n', errors.Select(e => e.Message))}\n```");
            }
            else
            {
                blocks.Add("The run failed for a reason that isn't reported here — check the build logs for details.");
            }
        }

        return string.Join("\n\n", blocks) + "\n";
    }
}
