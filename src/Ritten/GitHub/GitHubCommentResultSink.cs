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
    ICommentService comments
) : IWorkflowResultSink
{
    public Task Started(WorkflowJob job, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsPullRequest)
        {
            return Task.CompletedTask;
        }

        return comments.CreateOrUpdate($"## ⏳ {context.Title}\n\n{job.Name} job in progress…", cancellationToken);
    }

    public Task Publish(WorkflowReport report, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsPullRequest)
        {
            return Task.CompletedTask;
        }

        var markdown = renderer.Render(report);

        // Unlike the job summary, the comment lives away from the run, so it links back to the logs.
        if (options.Value.RunUrl is { } runUrl)
        {
            markdown = $"{markdown}\n[View the run logs]({runUrl})\n";
        }

        return comments.CreateOrUpdate(markdown, cancellationToken);
    }
}
