using Microsoft.Extensions.Options;
using Wolfe.Hamelin.GitHub;

namespace Wolfe.Hamelin.Reporting.Sinks;

/// <summary>
/// Publishes the report to a GitHub comment.
/// </summary>
internal class PullRequestCommentSink(IOptions<GitHubOptions> options, IPullRequestCommentService comments) : IReportSink
{
    public Task Publish(string markdown, CancellationToken cancellationToken = default) =>
        options.Value.IsPullRequest
            ? comments.CreateOrUpdate(markdown, cancellationToken)
            : Task.CompletedTask;
}
