using System.Diagnostics;
using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Runs;

public class DefaultWorkflowRunnerTelemetryTests : IDisposable
{
    private readonly TraceCollector _traces = new();

    public void Dispose() => _traces.Dispose();

    [Fact]
    public async Task Run_TracesTheJobWithEachStepAsItsChild()
    {
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            steps: [new TestStepA(), new TestStepB()],
            reporters: [spans],
            job: new WorkflowJob("Docker", "deploy", DryRun: true));

        await sut.Run(TestContext.Current.CancellationToken);

        var trace = _traces.Trace(spans.TraceId);
        var job = trace.Single(span => span.Parent is null);
        job.DisplayName.ShouldBe("Docker deploy");
        job.GetTagItem("ritten.workflow").ShouldBe("Docker");
        job.GetTagItem("ritten.job").ShouldBe("deploy");
        job.GetTagItem("ritten.dry_run").ShouldBe(true);
        job.GetTagItem("ritten.exit_code").ShouldBe(0);
        job.GetTagItem("cicd.pipeline.result").ShouldBe("success");
        job.Status.ShouldBe(ActivityStatusCode.Unset);

        var steps = trace.Where(span => span.ParentSpanId == job.SpanId).ToList();
        steps.Count.ShouldBe(2);
        steps.ShouldAllBe(step => step.DisplayName == "test step");
        steps.ShouldAllBe(step => (string?)step.GetTagItem("ritten.step.kind") == "work");
        steps.ShouldAllBe(step => (string?)step.GetTagItem("cicd.pipeline.task.run.result") == "success");
        steps.ShouldAllBe(step => (bool?)step.GetTagItem("ritten.step.continued") == true);
    }

    [Fact]
    public async Task Run_MarksTheFailedStepAndTheJob()
    {
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            steps: [new TestStepA { OnRun = _ => Task.FromResult(StepResult.Failed("Broken.")) }],
            reporters: [spans]);

        await sut.Run(TestContext.Current.CancellationToken);

        var trace = _traces.Trace(spans.TraceId);
        var job = trace.Single(span => span.Parent is null);
        var step = trace.Single(span => span.ParentSpanId == job.SpanId);
        step.Status.ShouldBe(ActivityStatusCode.Error);
        step.StatusDescription.ShouldBe("Broken.");
        step.GetTagItem("cicd.pipeline.task.run.result").ShouldBe("failure");
        step.GetTagItem("ritten.step.continued").ShouldBeNull();
        job.Status.ShouldBe(ActivityStatusCode.Error);
        job.StatusDescription.ShouldBe("test step: Broken.");
        job.GetTagItem("cicd.pipeline.result").ShouldBe("failure");
    }

    [Fact]
    public async Task Run_RecordsWhatAStepThrew()
    {
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            steps: [new TestStepA { OnRun = _ => throw new InvalidOperationException("Unexpected.") }],
            reporters: [spans]);

        await sut.Run(TestContext.Current.CancellationToken);

        var step = _traces.Trace(spans.TraceId).Single(span => span.Parent is not null);
        step.Events.ShouldHaveSingleItem().Name.ShouldBe("exception");
        step.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task Run_MarksAStepThatFinishedTheJobEarly()
    {
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            steps: [new TestStepA { OnRun = _ => Task.FromResult(StepResult.NothingToDo) }, new TestStepB()],
            reporters: [spans]);

        await sut.Run(TestContext.Current.CancellationToken);

        // The step after it never ran, so it has no span.
        var step = _traces.Trace(spans.TraceId).Single(span => span.Parent is not null);
        step.GetTagItem("ritten.step.continued").ShouldBe(false);
        step.GetTagItem("cicd.pipeline.task.run.result").ShouldBe("success");
    }

    [Fact]
    public async Task Run_NamesTheCiRunWhenTheRuntimeKnowsIt()
    {
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            reporters: [spans],
            context: new RunContext { Title = "grafana compose", Id = "4123", Url = "https://git.example/lab/actions/runs/88" });

        await sut.Run(TestContext.Current.CancellationToken);

        var job = _traces.Trace(spans.TraceId).Single();
        job.GetTagItem("cicd.pipeline.name").ShouldBe("grafana compose");
        job.GetTagItem("cicd.pipeline.run.id").ShouldBe("4123");
        job.GetTagItem("cicd.pipeline.run.url.full").ShouldBe("https://git.example/lab/actions/runs/88");
    }

    [Fact]
    public async Task Run_NamesNoCiRunOutsideCi()
    {
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(reporters: [spans]);

        await sut.Run(TestContext.Current.CancellationToken);

        var job = _traces.Trace(spans.TraceId).Single();
        job.GetTagItem("cicd.pipeline.name").ShouldBeNull();
        job.GetTagItem("cicd.pipeline.run.id").ShouldBeNull();
    }

    [Fact]
    public async Task Run_MakesEachSpanCurrentForTheReportersEitherSideOfIt()
    {
        // The promise a host relies on to add its own attributes: a reporter finds the job's span current around the
        // job, and the step's around the step.
        var spans = new CurrentSpans();
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [new TestStepA()], reporters: [spans]);

        await sut.Run(TestContext.Current.CancellationToken);

        spans.Seen.Select(seen => seen.Hook).ShouldBe(["job started", "step started", "step completed", "job completed"]);
        spans.Seen.Select(seen => seen.Span).ShouldBe(["Test verify", "test step", "test step", "Test verify"]);
    }

    [Fact]
    public async Task Run_MakesTheStepsSpanTheParentOfWhatItDoes()
    {
        var spans = new CurrentSpans();
        string? parent = null;
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            steps:
            [
                new TestStepA
                {
                    OnRun = _ =>
                    {
                        parent = Activity.Current?.DisplayName;
                        return Task.FromResult(StepResult.Successful);
                    }
                }
            ],
            reporters: [spans]);

        await sut.Run(TestContext.Current.CancellationToken);

        parent.ShouldBe("test step");
    }

    /// <summary>
    /// Notes the span current at each hook, and the trace the run started.
    /// </summary>
    private sealed class CurrentSpans : IWorkflowProgress
    {
        public List<(string Hook, string? Span)> Seen { get; } = [];

        public ActivityTraceId TraceId { get; private set; }

        public Task OnWorkflowStarted(WorkflowJob job, CancellationToken cancellationToken = default)
        {
            TraceId = Activity.Current?.TraceId ?? default;
            return Note("job started");
        }

        public Task OnStepStarted(Step step, CancellationToken cancellationToken = default) => Note("step started");

        public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken = default) => Note("step completed");

        public Task OnWorkflowCompleted(WorkflowResult result, CancellationToken cancellationToken = default) => Note("job completed");

        private Task Note(string hook)
        {
            Seen.Add((hook, Activity.Current?.DisplayName));
            return Task.CompletedTask;
        }
    }
}
