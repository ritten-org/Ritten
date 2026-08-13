using Ritten.Reporting.Sinks;

namespace Ritten.Runtimes.GitHubActions;

/// <summary>
/// Publishes the report to the GitHub Actions job summary via <c>GITHUB_STEP_SUMMARY</c>.
/// Does nothing when the pipeline is not running in GitHub Actions.
/// </summary>
internal class GitHubReportSink(IGitHubActionsRuntime runtime) : IReportSink
{
    public async Task Publish(string markdown, CancellationToken cancellationToken = default)
    {
        if (!runtime.IsEnabled)
        {
            return;
        }

        if (runtime.SummaryFile is not { } path)
        {
            return;
        }

        await File.AppendAllTextAsync(path, markdown, cancellationToken);
    }
}
