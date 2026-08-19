using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;

namespace Ritten.Tests.Engine.Runs;

public class DefaultWorkflowRunnerTests
{
    [Fact]
    public async Task RunWorkflow_WithSteps_RunsStepsInOrder()
    {
        // Arrange
        var journal = new List<object>();
        var step1 = new TestStepA { Journal = journal };
        var step2 = new TestStepB { Journal = journal };
        var step3 = new TestStepC { Journal = journal };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe([step1, step2, step3]);
    }

    [Fact]
    public async Task RunWorkflow_StoppedOnError_StopsExecution()
    {
        // Arrange
        var step1 = new TestStepA();
        var step2 = new TestStepB { OnRun = _ => throw new Exception("Broken.") };
        var step3 = new TestStepC();

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        step1.Runs.ShouldBe(1);
        step2.Runs.ShouldBe(1);
        step3.Runs.ShouldBe(0);
    }

    [Fact]
    public async Task RunWorkflow_TokenCancelled_ReturnsCorrectExitCode()
    {
        // Arrange
        var step1 = new TestStepA();

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step1]);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var summary = await sut.Run(cts.Token);

        // Assert
        step1.Runs.ShouldBe(0);
        summary.ExitCode.ShouldBe(ExitCode.Cancelled);
    }

    [Fact]
    public async Task RunWorkflow_StepThrowsOperationCanceled_ReportsCancellationNotFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var step = new TestStepA
        {
            OnRun = _ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            }
        };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step]);

        // Act
        var summary = await sut.Run(cts.Token);

        // Assert
        summary.ExitCode.ShouldBe(ExitCode.Cancelled);
        summary.Steps.ShouldHaveSingleItem().Result.ShouldBe(StepResult.StoppedAfterCancel);
    }

    [Fact]
    public async Task RunWorkflow_StepFailsButAsksToContinue_StillFailsTheWorkflow()
    {
        // Arrange
        var failed = new StepResult(ExitCode.Failed, Continue: true, ["Failed, but not fatally."]);
        var step1 = new TestStepA { OnRun = _ => Task.FromResult(failed) };
        var step2 = new TestStepB();

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step1, step2]);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        step2.Runs.ShouldBe(1);
        summary.ExitCode.ShouldBe(ExitCode.Failed);
        summary.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task RunWorkflow_NoSteps_Succeeds()
    {
        // Arrange
        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: []);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(ExitCode.Success);
    }

    [Fact]
    public async Task RunWorkflow_StepThrows_WritesTheExceptionDetailToTheLog()
    {
        // Arrange
        var log = Substitute.For<IWorkflowLog>();
        var step = new TestStepA { OnRun = _ => throw new InvalidOperationException("Something broke.") };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step], log: log);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.Steps.ShouldHaveSingleItem().Result.Errors.ShouldHaveSingleItem("Something broke.");
        log.Received().Log(
            WorkflowLogLevel.Verbose,
            Arg.Any<string>(),
            Arg.Is<Exception>(e => e.Message == "Something broke."));
    }

    [Fact]
    public async Task RunWorkflow_ReporterError_WarnsWithoutFailingTheWorkflow()
    {
        // Arrange
        var log = Substitute.For<IWorkflowLog>();
        var reporter = Substitute.For<IWorkflowProgress>();
        reporter.OnWorkflowStarted(Arg.Any<WorkflowJob>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Reporter is broken."));

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(
            reporters: [reporter],
            steps: [new TestStepA()],
            log: log
        );

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(ExitCode.Success);
        log.Received().Log(
            WorkflowLogLevel.Warning,
            Arg.Any<string>(),
            Arg.Is<Exception>(e => e.Message == "Reporter is broken."));
    }

    [Fact]
    public async Task RunWorkflow_StoppedOnError_ReturnsCorrectExitCode()
    {
        // Arrange
        var step1 = new TestStepA();
        var step2 = new TestStepB { OnRun = _ => throw new Exception("Broken.") };
        var step3 = new TestStepC();

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(ExitCode.Failed);
    }

    [Fact]
    public async Task RunWorkflow_StoppedOnError_ExposesTheFailingStepAsTheFailure()
    {
        // Arrange
        var step1 = new TestStepA();
        var step2 = new TestStepB { OnRun = _ => throw new Exception("Broken.") };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step1, step2]);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        var failure = summary.FailedStep.ShouldNotBeNull();
        failure.Step.StepType.ShouldBe(typeof(TestStepB));
        failure.Result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldBe("Broken.");
    }

    [Fact]
    public async Task RunWorkflow_WithReporters_CallsOnWorkflowStartedBeforeSteps()
    {
        // Arrange
        var journal = new List<object>();
        var reporter = Substitute.For<IWorkflowProgress>();
        reporter.OnWorkflowStarted(Arg.Any<WorkflowJob>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("workflow started");
                return Task.CompletedTask;
            });
        var step = new TestStepA { Journal = journal };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(reporters: [reporter], steps: [step]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe(["workflow started", step]);
    }

    [Fact]
    public async Task RunWorkflow_WithReporters_CallsOnWorkflowCompletedAfterSteps()
    {
        // Arrange
        var journal = new List<object>();
        var reporter = Substitute.For<IWorkflowProgress>();
        reporter.OnWorkflowCompleted(Arg.Any<WorkflowResult>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("workflow completed");
                return Task.CompletedTask;
            });
        var step = new TestStepA { Journal = journal };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [step], reporters: [reporter]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe([step, "workflow completed"]);
    }

    [Fact]
    public async Task RunWorkflow_WithReporters_CallsStepLifecycleAroundEachStep()
    {
        // Arrange
        var journal = new List<object>();
        var reporter = Substitute.For<IWorkflowProgress>();
        reporter.OnStepStarted(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("step started");
                return Task.CompletedTask;
            });
        reporter.OnStepCompleted(Arg.Any<Step>(), Arg.Any<StepResult>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("step completed");
                return Task.CompletedTask;
            });
        var step = new TestStepA { Journal = journal };

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(reporters: [reporter], steps: [step]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe(["step started", step, "step completed"]);
    }

    [Fact]
    public async Task RunWorkflow_ProducingStep_FeedsTheValueToTheNextStepsParameter()
    {
        // The whole contract in one round trip: a returned value arrives as the next parameter.
        var producer = new ProducingStep();
        var consumer = new ConsumingStep();

        var sut = DefaultWorkflowRunnerHelpers.CreateRunner(steps: [producer, consumer]);
        var summary = await sut.Run(CancellationToken.None);

        summary.IsSuccess.ShouldBeTrue();
        consumer.Received.ShouldBe("the produced value");
    }

    private sealed class ProducedValue(string text)
    {
        public string Text => text;
    }

    [Step("producer", StepKind.Work)]
    private sealed class ProducingStep
    {
        // Synchronous on purpose: a directly returned value must reach the next step the same way.
        public StepResult<ProducedValue> Run() => new ProducedValue("the produced value");
    }

    [Step("consumer", StepKind.Work)]
    private sealed class ConsumingStep
    {
        public string? Received { get; private set; }

        public Task<StepResult> Run(ProducedValue value, CancellationToken cancellationToken)
        {
            Received = value.Text;
            return Task.FromResult(StepResult.Successful);
        }
    }
}
