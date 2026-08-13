using Ritten.Contracts.Runtime;

namespace Ritten.Reporting.Sinks;

/// <summary>
/// Publishes the report to the GitHub Actions job summary.
/// </summary>
internal class JobSummarySink(IRuntimeContext context, IRuntimeCommands commands) : IReportSink
{
    public async Task Publish(string markdown, CancellationToken cancellationToken = default)
    {
        if (!context.IsCI)
        {
            return;
        }

        await commands.AppendJobSummary(markdown, cancellationToken);
    }
}
