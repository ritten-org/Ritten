using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;

namespace Ritten.GitHub;

/// <summary>
/// Publishes the report to a GitHub comment, with a pending comment when the run starts.
/// </summary>
internal class GitHubCommentResultSink(
    MarkdownReportRenderer renderer,
    RunContext context,
    IOptions<GitHubActionsOptions> options,
    IGitHubCommentService comments
) : IWorkflowResultSink
{
    public Task Started(WorkflowJob job, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsPullRequest)
        {
            return Task.CompletedTask;
        }

        return comments.CreateOrUpdate(WithRunLogs($"## ⏳ {context.Title}\n\n{job.Name} job in progress…"), cancellationToken);
    }

    public Task Publish(WorkflowReport report, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsPullRequest)
        {
            return Task.CompletedTask;
        }

        return comments.CreateOrUpdate(WithRunLogs(renderer.Render(report)), cancellationToken);
    }

    /// <summary>
    /// Unlike the job summary, the comment lives away from the run, so it links back to the logs —
    /// never more usefully than while the run is still going and the logs are all there is to see.
    /// </summary>
    private string WithRunLogs(string markdown) =>
        options.Value.RunUrl is { } runUrl ? $"{markdown}\n[View the run logs]({runUrl})\n" : markdown;
}
