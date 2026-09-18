using Microsoft.Extensions.Options;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;

namespace Ritten.Forgejo;

/// <summary>
/// The build report, appended to the job summary when the runner offers one.
/// </summary>
internal sealed class ForgejoJobSummaryResultSink(MarkdownReportRenderer renderer, IOptions<ForgejoActionsOptions> options) : IWorkflowResultSink
{
    public async Task Publish(WorkflowReport report, CancellationToken cancellationToken = default)
    {
        if (options.Value.SummaryFile is not { } path)
        {
            return;
        }

        await File.AppendAllTextAsync(path, renderer.Render(report), cancellationToken);
    }
}
