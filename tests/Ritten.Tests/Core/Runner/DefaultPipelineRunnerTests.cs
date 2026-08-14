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
        var step1 = PipelineStepHelpers.CreateMock();
        var step2 = PipelineStepHelpers.CreateMock();
        var step3 = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            step1.Run(Arg.Any<CancellationToken>());
            step2.Run(Arg.Any<CancellationToken>());
            step3.Run(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_StoppedOnError_StopsExecution()
    {
        // Arrange
        var step1 = PipelineStepHelpers.CreateMock();
        var step2 = PipelineStepHelpers.CreateMock();
        step2.Run(Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();
        var step3 = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2, step3]);

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        await step1.Received().Run(Arg.Any<CancellationToken>());
        await step2.Received().Run(Arg.Any<CancellationToken>());
        await step3.DidNotReceive().Run(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunPipeline_TokenCancelled_ReturnsCorrectExitCode()
    {
        // Arrange
        var step1 = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1]);

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var summary = await sut.Run(cts.Token);

        // Assert
        await step1.DidNotReceive().Run(Arg.Any<CancellationToken>());
        summary.ExitCode.ShouldBe(PipelineExitCodes.Cancelled);
    }

    [Fact]
    public async Task RunPipeline_StepThrowsOperationCanceled_ReportsCancellationNotFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var step = PipelineStepHelpers.CreateMock();
        step.When(s => s.Run(Arg.Any<CancellationToken>())).Do(_ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        });

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
        var step1 = PipelineStepHelpers.CreateMock();
        step1.Run(Arg.Any<CancellationToken>()).Returns(failed);
        var step2 = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2]);

        // Act
        var summary = await sut.Run(CancellationToken.None);

        // Assert
        await step2.Received().Run(Arg.Any<CancellationToken>());
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
        var step = PipelineStepHelpers.CreateMock();
        step.Run(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Something broke."));

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
            steps: [PipelineStepHelpers.CreateMock()],
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
        var step1 = PipelineStepHelpers.CreateMock();
        var step2 = PipelineStepHelpers.CreateMock();
        step2.Run(Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();
        var step3 = PipelineStepHelpers.CreateMock();

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
        var reporter = Substitute.For<IProgressReporter>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            reporters: [reporter],
            steps: [step]
        );

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            reporter.OnPipelineStarted(Arg.Any<PipelineJob>(), Arg.Any<CancellationToken>());
            step.Run(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_WithReporters_CallsOnPipelineCompletedAfterSteps()
    {
        // Arrange
        var reporter = Substitute.For<IProgressReporter>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            steps: [step],
            reporters: [reporter]
        );

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            step.Run(Arg.Any<CancellationToken>());
            reporter.OnPipelineCompleted(Arg.Any<PipelineResult>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_WithReporters_CallsStepLifecycleAroundEachStep()
    {
        // Arrange
        var reporter = Substitute.For<IProgressReporter>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            reporters: [reporter],
            steps: [step]
        );

        // Act
        await sut.Run(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            reporter.OnStepStarted(Arg.Any<IPipelineStep>(), Arg.Any<CancellationToken>());
            step.Run(Arg.Any<CancellationToken>());
            reporter.OnStepCompleted(Arg.Any<IPipelineStep>(), Arg.Any<StepResult>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_ReporterError_StillRunsPipeline()
    {
        // Arrange
        var reporter = Substitute.For<IProgressReporter>();
        reporter.OnPipelineStarted(Arg.Any<PipelineJob>(), Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();

        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            reporters: [reporter],
            steps: [step]
        );

        // Act
        var act = () => sut.Run(CancellationToken.None);

        // Assert
        await act.ShouldNotThrowAsync();
        await step.Received().Run(Arg.Any<CancellationToken>());
    }
}
