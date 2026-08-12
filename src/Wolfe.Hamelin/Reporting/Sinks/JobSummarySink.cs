namespace Wolfe.Hamelin.Reporting.Sinks;

/// <summary>
/// Publishes the report to the GitHub Actions job summary.
/// </summary>
internal class JobSummarySink : IReportSink
{
    // Writes the file directly rather than going through IGitHubActionsCommands.AppendJobSummary:
    // Hamelin 3.0.0 reads GITHUB_JOB_SUMMARY, but the variable GitHub Actions provides is
    // GITHUB_STEP_SUMMARY.
    public async Task Publish(string markdown, CancellationToken cancellationToken = default)
    {
        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is not { Length: > 0 } summaryFile)
        {
            return;
        }

        await File.AppendAllTextAsync(summaryFile, markdown, cancellationToken);
    }
}
