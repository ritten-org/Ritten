using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;

namespace Ritten.Tests.Reporting;

public class WorkflowReportPublisherTests
{
    private static readonly WorkflowJob Job = new("Test", "check", DryRun: false);

    private readonly IWorkflowResultSink _first = Substitute.For<IWorkflowResultSink>();
    private readonly IWorkflowResultSink _second = Substitute.For<IWorkflowResultSink>();

    [Fact]
    public async Task OnWorkflowStarted_AnnouncesTheRunToEverySink()
    {
        await Publisher().OnWorkflowStarted(Job, TestContext.Current.CancellationToken);

        await _first.Received().Started(Job, TestContext.Current.CancellationToken);
        await _second.Received().Started(Job, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnWorkflowCompleted_ComposesTheReportOnceForEverySink()
    {
        var report = new WorkflowReport();
        report.Section("Build").Failure("The solution failed to build.");

        await Publisher(report).OnWorkflowCompleted(new WorkflowResult(ExitCode.Failed, []), TestContext.Current.CancellationToken);

        await _first.Received().Publish(
            Arg.Is<Report>(r => r.Title == "Test" && !r.Succeeded && r.Sections.Count == 1),
            TestContext.Current.CancellationToken);
        await _second.Received().Publish(Arg.Any<Report>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AFailingSinkNeverBreaksTheOthers()
    {
        // The publisher owns the resilience, so no sink has to guard its own destination.
        _first.Started(Arg.Any<WorkflowJob>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("boom"));
        _first.Publish(Arg.Any<Report>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException("boom"));
        var publisher = Publisher();

        await publisher.OnWorkflowStarted(Job, TestContext.Current.CancellationToken);
        await publisher.OnWorkflowCompleted(new WorkflowResult(ExitCode.Success, []), TestContext.Current.CancellationToken);

        await _second.Received().Started(Job, Arg.Any<CancellationToken>());
        await _second.Received().Publish(Arg.Any<Report>(), Arg.Any<CancellationToken>());
    }

    private WorkflowReportPublisher Publisher(IWorkflowReport? report = null) =>
        new(Substitute.For<IWorkflowLog>(), new RunContext { Title = "Test" }, report ?? new WorkflowReport(), [_first, _second]);
}
