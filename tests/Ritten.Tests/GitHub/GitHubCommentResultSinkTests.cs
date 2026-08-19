using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.GitHub;
using Ritten.Reporting;

namespace Ritten.Tests.GitHub;

public class GitHubCommentResultSinkTests
{
    private readonly ICommentService _comments = Substitute.For<ICommentService>();
    private readonly GitHubActionsOptions _options = new() { PullRequestNumber = 42 };

    [Fact]
    public async Task Started_PostsThePendingComment()
    {
        // The pending half of the sink: the pull request shows the run is underway before any
        // result exists, in the same comment the finished report will replace.
        await Sink().Started(new WorkflowJob("Ritten", "check", DryRun: false), TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate("## ⏳ Ritten\n\ncheck job in progress…", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Started_DoesNothingOutsideOfAPullRequest()
    {
        _options.PullRequestNumber = null;

        await Sink().Started(new WorkflowJob("Ritten", "check", DryRun: false), TestContext.Current.CancellationToken);

        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_RendersTheReportAndAppendsALinkToTheRunLogs()
    {
        _options.RunUrl = "https://github.com/example/repo/actions/runs/987654";

        await Sink().Publish(Success, TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(
            "## ✅ Ritten\n\n[View the run logs](https://github.com/example/repo/actions/runs/987654)\n",
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_LeavesTheReportAloneWhenTheRunUrlIsUnknown()
    {
        await Sink().Publish(Success, TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate("## ✅ Ritten\n", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_DoesNothingOutsideOfAPullRequest()
    {
        _options.PullRequestNumber = null;

        await Sink().Publish(Success, TestContext.Current.CancellationToken);

        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, TestContext.Current.CancellationToken);
    }

    private static WorkflowReport Success => new("Ritten", Succeeded: true, []);

    private GitHubCommentResultSink Sink() =>
        new(new MarkdownReportRenderer(), new RunContext { Title = "Ritten" }, Options.Create(_options), _comments);
}
