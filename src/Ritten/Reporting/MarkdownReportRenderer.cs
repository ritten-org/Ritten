using Ritten.Core;

namespace Ritten.Reporting;

internal class MarkdownReportRenderer
{
    /// <summary>
    /// Renders the report as GitHub-flavored Markdown.
    /// </summary>
    /// <param name="title">The title of the report.</param>
    /// <param name="succeeded">Whether the run succeeded.</param>
    /// <param name="sections">The sections the steps authored.</param>
    /// <param name="failure">The failing step and its result, used when no section reports a failure.</param>
    public string Render(string title, bool succeeded, IReadOnlyList<ReportSection> sections, StepOutcome? failure = null)
    {
        var blocks = new List<string> { $"## {(succeeded ? "✅" : "❌")} {title}" };

        foreach (var section in sections.Where(s => s.Entries.Count > 0))
        {
            blocks.Add($"### {section.Tone.Icon()} {section.Title}");
            blocks.AddRange(section.Entries.Select(e => e.ToMarkdown()));
        }

        // A failing step that authored nothing still has its errors on the step result;
        // fall back to those before admitting the reader has to go digging in the logs.
        if (!succeeded && sections.All(s => s.Tone != ReportTone.Failure))
        {
            if (failure?.Result.Errors is { Count: > 0 } errors)
            {
                blocks.Add($"### {ReportTone.Failure.Icon()} {failure.Step.Name}");
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
