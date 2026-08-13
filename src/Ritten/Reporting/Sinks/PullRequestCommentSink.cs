using Microsoft.Extensions.Options;
using Ritten.GitHub;

namespace Ritten.Reporting.Sinks;

/// <summary>
/// Publishes the report to a GitHub comment.
/// </summary>
internal class PullRequestCommentSink(IOptions<GitHubOptions> options, ICommentService comments) : IReportSink
{
    public Task Publish(string markdown, CancellationToken cancellationToken = default) =>
        options.Value.IsPullRequest
            ? comments.CreateOrUpdate(markdown, cancellationToken)
            : Task.CompletedTask;
}
