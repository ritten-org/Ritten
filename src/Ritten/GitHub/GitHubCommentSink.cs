using Microsoft.Extensions.Options;
using Ritten.Reporting.Sinks;

namespace Ritten.GitHub;

/// <summary>
/// Publishes the report to a GitHub comment.
/// </summary>
internal class GitHubCommentSink(IOptions<GitHubOptions> options, ICommentService comments) : IReportSink
{
    public Task Publish(string markdown, CancellationToken cancellationToken = default) => options.Value.IsPullRequest
        ? comments.CreateOrUpdate(markdown, cancellationToken)
        : Task.CompletedTask;
}
