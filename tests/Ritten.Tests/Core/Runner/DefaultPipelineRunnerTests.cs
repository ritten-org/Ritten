using Ritten.Contracts;
using Ritten.Core;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Core.Runner;

public class DefaultPipelineRunnerTests
{
    [Fact]
    public async Task RunPipeline_WithSteps_RunsStepsInOrder()
    {
        // Arrange
        var journal = new List<object>();
        var step1 = new TestStepA { Journal = journal };
        var step2 = new TestStepB { Journal = journal };
        var step3 = new TestStepC { Journal = journal };

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe([step1, step2, step3]);
    }

    [Fact]
    public async Task RunPipeline_StoppedOnError_StopsExecution()
    {
        // Arrange
        var step1 = new TestStepA();
        var step2 = new TestStepB { OnRun = _ => throw new Exception("Broken.") };
        var step3 = new TestStepC();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        step1.Runs.ShouldBe(1);
        step2.Runs.ShouldBe(1);
        step3.Runs.ShouldBe(0);
    }

    [Fact]
    public async Task RunPipeline_TokenCancelled_ReturnsCorrectExitCode()
    {
        // Arrange
        var step1 = new TestStepA();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1]);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var summary = await sut.Run(cts.Token);

        // Assert
        step1.Runs.ShouldBe(0);
        summary.ExitCode.ShouldBe(PipelineExitCodes.Cancelled);
    }

    [Fact]
    public async Task RunPipeline_StepThrowsOperationCanceled_ReportsCancellationNotFailure()
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

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step]);

        // Act
        var summary = await sut.Run(cts.Token);

        // Assert
        summary.ExitCode.ShouldBe(PipelineExitCodes.Cancelled);
        summary.Steps.ShouldHaveSingleItem().ShouldBe(StepResult.StoppedAfterCancel);
    }

    [Fact]
    public async Task RunPipeline_StepFailsButAsksToContinue_StillFailsThePipeline()
    {
        // Arrange
        var failed = new StepResult(PipelineExitCodes.Failed, Continue: true, ["Failed, but not fatally."]);
        var step1 = new TestStepA { OnRun = _ => Task.FromResult(failed) };
        var step2 = new TestStepB();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2]);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        step2.Runs.ShouldBe(1);
        summary.ExitCode.ShouldBe(PipelineExitCodes.Failed);
        summary.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task RunPipeline_NoSteps_Succeeds()
    {
        // Arrange
        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: []);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(PipelineExitCodes.Success);
    }

    [Fact]
    public async Task RunPipeline_StepThrows_WritesTheExceptionDetailToTheLog()
    {
        // Arrange
        var log = Substitute.For<IPipelineLog>();
        var step = new TestStepA { OnRun = _ => throw new InvalidOperationException("Something broke.") };

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step], log: log);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.Steps.ShouldHaveSingleItem().Errors.ShouldHaveSingleItem("Something broke.");
        log.Received().Log(
            PipelineLogLevel.Verbose,
            Arg.Any<string>(),
            Arg.Is<Exception>(e => e.Message == "Something broke."));
    }

    [Fact]
    public async Task RunPipeline_ReporterError_WarnsWithoutFailingThePipeline()
    {
        // Arrange
        var log = Substitute.For<IPipelineLog>();
        var reporter = Substitute.For<IProgressReporter>();
        reporter.OnPipelineStarted(Arg.Any<PipelineJob>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Reporter is broken."));

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            reporters: [reporter],
            steps: [new TestStepA()],
            log: log
        );

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(PipelineExitCodes.Success);
        log.Received().Log(
            PipelineLogLevel.Warning,
            Arg.Any<string>(),
            Arg.Is<Exception>(e => e.Message == "Reporter is broken."));
    }

    [Fact]
    public async Task RunPipeline_StoppedOnError_ReturnsCorrectExitCode()
    {
        // Arrange
        var step1 = new TestStepA();
        var step2 = new TestStepB { OnRun = _ => throw new Exception("Broken.") };
        var step3 = new TestStepC();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(PipelineExitCodes.Failed);
    }

    [Fact]
    public async Task RunPipeline_WithReporters_CallsOnPipelineStartedBeforeSteps()
    {
        // Arrange
        var journal = new List<object>();
        var reporter = Substitute.For<IProgressReporter>();
        reporter.OnPipelineStarted(Arg.Any<PipelineJob>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("pipeline started");
                return Task.CompletedTask;
            });
        var step = new TestStepA { Journal = journal };

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(reporters: [reporter], steps: [step]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe(["pipeline started", step]);
    }

    [Fact]
    public async Task RunPipeline_WithReporters_CallsOnPipelineCompletedAfterSteps()
    {
        // Arrange
        var journal = new List<object>();
        var reporter = Substitute.For<IProgressReporter>();
        reporter.OnPipelineCompleted(Arg.Any<PipelineResult>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("pipeline completed");
                return Task.CompletedTask;
            });
        var step = new TestStepA { Journal = journal };

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step], reporters: [reporter]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe([step, "pipeline completed"]);
    }

    [Fact]
    public async Task RunPipeline_WithReporters_CallsStepLifecycleAroundEachStep()
    {
        // Arrange
        var journal = new List<object>();
        var reporter = Substitute.For<IProgressReporter>();
        reporter.OnStepStarted(Arg.Any<JobStep>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("step started");
                return Task.CompletedTask;
            });
        reporter.OnStepCompleted(Arg.Any<JobStep>(), Arg.Any<StepResult>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                journal.Add("step completed");
                return Task.CompletedTask;
            });
        var step = new TestStepA { Journal = journal };

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(reporters: [reporter], steps: [step]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        journal.ShouldBe(["step started", step, "step completed"]);
    }

    [Fact]
    public async Task RunPipeline_ProducingStep_FeedsTheValueToTheNextStepsParameter()
    {
        // The whole contract in one round trip: a returned value arrives as the next parameter.
        var producer = new ProducingStep();
        var consumer = new ConsumingStep();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [producer, consumer]);
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
