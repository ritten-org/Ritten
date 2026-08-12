using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Reporting.GitHub;

namespace Wolfe.Hamelin.Build.Reporting.Sinks;

public class PullRequestCommentSink(IOptions<GitHubOptions> options, IPullRequestCommentService comments) : IReportSink
{
    public Task Publish(string markdown, CancellationToken cancellationToken = default) =>
        options.Value.IsPullRequest
            ? comments.CreateOrUpdate(markdown, cancellationToken)
            : Task.CompletedTask;
}
