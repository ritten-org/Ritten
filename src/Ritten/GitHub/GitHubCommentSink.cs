using Microsoft.Extensions.Options;
using Ritten.Reporting.Sinks;

namespace Ritten.GitHub;

/// <summary>
/// Publishes the report to a GitHub comment.
/// </summary>
internal class GitHubCommentSink(IOptions<GitHubOptions> options, ICommentService comments) : IReportSink
{
    public Task Publish(string markdown, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsPullRequest)
        {
            return Task.CompletedTask;
        }

        // Unlike the job summary, the comment lives away from the run, so it links back to the logs.
        if (options.Value.RunUrl is { } runUrl)
        {
            markdown = $"{markdown}\n[View the run logs]({runUrl})\n";
        }

        return comments.CreateOrUpdate(markdown, cancellationToken);
    }
}
