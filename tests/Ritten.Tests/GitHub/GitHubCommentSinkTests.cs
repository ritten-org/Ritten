using Microsoft.Extensions.Options;
using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

public class GitHubCommentSinkTests
{
    private readonly ICommentService _comments = Substitute.For<ICommentService>();
    private readonly GitHubOptions _options = new() { PullRequestNumber = 42 };

    [Fact]
    public async Task Publish_AppendsALinkToTheRunLogs()
    {
        _options.RunUrl = "https://github.com/example/repo/actions/runs/987654";

        await Sink().Publish("## ✅ Ritten\n", TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(
            "## ✅ Ritten\n\n[View the run logs](https://github.com/example/repo/actions/runs/987654)\n",
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_LeavesTheReportAloneWhenTheRunUrlIsUnknown()
    {
        await Sink().Publish("## ✅ Ritten\n", TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate("## ✅ Ritten\n", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_DoesNothingOutsideOfAPullRequest()
    {
        _options.PullRequestNumber = null;

        await Sink().Publish("## ✅ Ritten\n", TestContext.Current.CancellationToken);

        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, TestContext.Current.CancellationToken);
    }

    private GitHubCommentSink Sink() => new(Options.Create(_options), _comments);
}
