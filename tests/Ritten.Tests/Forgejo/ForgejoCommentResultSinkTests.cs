using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.Forgejo;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Forgejo;

public class ForgejoCommentResultSinkTests
{
    private static readonly ReportKey Key = new("Ritten");
    private static readonly WorkflowJob Job = new("Ritten", "check");

    private readonly IForgejoCommentService _comments = Substitute.For<IForgejoCommentService>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly ForgejoActionsOptions _options = new() { PullRequestNumber = 42 };

    public ForgejoCommentResultSinkTests() => _comments.Unavailable.Returns((string?)null);

    [Fact]
    public async Task SaysWhyItCantCommentAndPostsNothing()
    {
        _comments.Unavailable.Returns("No API token in the environment.");
        var sink = Sink();

        await sink.Started(Job, TestContext.Current.CancellationToken);
        await sink.Publish(Success, TestContext.Current.CancellationToken);

        _log.Received().Log(WorkflowLogLevel.Detail, "No API token in the environment.");
        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Started_PostsThePendingComment()
    {
        // The pending half of the sink: the pull request shows the run is underway before any
        // result exists, in the same comment the finished report will replace.
        await Sink().Started(new WorkflowJob("Ritten", "check", DryRun: false), TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(Key, "## ⏳ Ritten\n\ncheck job in progress…", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Started_TitlesThePendingCommentWithTheRunsSubject()
    {
        await Sink().Started(new WorkflowJob("Ritten", "check", Subject: "caddy/infrastructure"), TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(new ReportKey("Ritten", "caddy/infrastructure"), "## ⏳ Ritten · caddy/infrastructure\n\ncheck job in progress…", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Started_LinksToTheRunLogsWhileThereIsNothingElseToShow()
    {
        _options.RunUrl = "https://code.example.com/tom/lab/actions/runs/42";

        await Sink().Started(new WorkflowJob("Ritten", "check", DryRun: false), TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(
            Key,
            "## ⏳ Ritten\n\ncheck job in progress…\n[View the run logs](https://code.example.com/tom/lab/actions/runs/42)\n",
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Started_DoesNothingOutsideOfAPullRequest()
    {
        _options.PullRequestNumber = null;

        await Sink().Started(new WorkflowJob("Ritten", "check", DryRun: false), TestContext.Current.CancellationToken);

        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_RendersTheReportAndAppendsALinkToTheRunLogs()
    {
        _options.RunUrl = "https://code.example.com/tom/lab/actions/runs/42";

        await Sink().Publish(Success, TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(
            Key,
            "## ✅ Ritten\n\n[View the run logs](https://code.example.com/tom/lab/actions/runs/42)\n",
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_LeavesTheReportAloneWhenTheRunUrlIsUnknown()
    {
        await Sink().Publish(Success, TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(Key, "## ✅ Ritten\n", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_DoesNothingOutsideOfAPullRequest()
    {
        _options.PullRequestNumber = null;

        await Sink().Publish(Success, TestContext.Current.CancellationToken);

        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_TakesThePendingCommentBackWhenTheRunHadNothingToSay()
    {
        // A check whose component the pull request did not touch stops at its gate with an
        // empty report; left as a comment, every check would comment on every pull request.
        await Sink().Publish(Silent, TestContext.Current.CancellationToken);

        await _comments.Received().Delete(Key, TestContext.Current.CancellationToken);
        await _comments.DidNotReceiveWithAnyArgs().CreateOrUpdate(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Publish_StillReportsAnEarlyStopThatSaidSomething()
    {
        // Stopping early is not the same as having nothing to say: a plan with no changes
        // stops the job and is worth reading.
        var section = new ReportSection("Infrastructure");
        section.Success("No changes.");

        await Sink().Publish(Silent with { Sections = [section] }, TestContext.Current.CancellationToken);

        await _comments.Received().CreateOrUpdate(Arg.Any<ReportKey>(), Arg.Any<string>(), TestContext.Current.CancellationToken);
        await _comments.DidNotReceive().Delete(Key, TestContext.Current.CancellationToken);
    }

    private static WorkflowReport Success => new("Ritten", Succeeded: true, []);

    private static WorkflowReport Silent =>
        new("Ritten", Succeeded: true, [], StoppedAt: new StepOutcome(Step.FromType<FirstStep>(), StepResult.NothingToDo));

    private ForgejoCommentResultSink Sink() =>
        new(new MarkdownReportRenderer(), new RunContext { Title = "Ritten" }, Job, Options.Create(_options), _comments, _log);
}
