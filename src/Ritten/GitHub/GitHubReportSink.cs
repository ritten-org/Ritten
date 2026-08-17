using Microsoft.Extensions.Options;
using Ritten.Reporting.Sinks;

namespace Ritten.GitHub;

/// <summary>
/// Publishes the report to the GitHub Actions job summary via <c>GITHUB_STEP_SUMMARY</c>.
/// Does nothing when the runner provides no summary file.
/// </summary>
internal class GitHubReportSink(IOptions<GitHubActionsOptions> options) : IReportSink
{
    public async Task Publish(string markdown, CancellationToken cancellationToken = default)
    {
        if (options.Value.SummaryFile is not { } path)
        {
            return;
        }

        await File.AppendAllTextAsync(path, markdown, cancellationToken);
    }
}
