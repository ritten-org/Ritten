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
        await sut.RunPipeline(CancellationToken.None);

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
        await sut.RunPipeline(CancellationToken.None);

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
        var summary = await sut.RunPipeline(cts.Token);

        // Assert
        await step1.DidNotReceive().Run(Arg.Any<CancellationToken>());
        summary.ExitCode.ShouldBe(PipelineExitCodes.StoppedAfterCancel);
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
        var summary = await sut.RunPipeline(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(PipelineExitCodes.StoppedOnError);
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
        await sut.RunPipeline(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            reporter.OnPipelineStarted(Arg.Any<Pipeline>(), Arg.Any<CancellationToken>());
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
        await sut.RunPipeline(CancellationToken.None);

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
        await sut.RunPipeline(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            reporter.OnStepStarted(Arg.Any<IPipelineStep>(), Arg.Any<CancellationToken>());
            step.Run(Arg.Any<CancellationToken>());
            reporter.OnStepCompleted(Arg.Any<StepResult>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_ReporterError_StillRunsPipeline()
    {
        // Arrange
        var reporter = Substitute.For<IProgressReporter>();
        reporter.OnPipelineStarted(Arg.Any<Pipeline>(), Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();

        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            reporters: [reporter],
            steps: [step]
        );

        // Act
        var act = () => sut.RunPipeline(CancellationToken.None);

        // Assert
        await act.ShouldNotThrowAsync();
        await step.Received().Run(Arg.Any<CancellationToken>());
    }
}
