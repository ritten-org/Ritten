using Ritten.Contracts;
using Ritten.Contracts.Hooks;
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
    public async Task RunPipeline_CustomExitCode_SetsExitCodeToCustomExitCode()
    {
        // Arrange
        var context = Substitute.For<IPipelineContext>();
        var step1 = PipelineStepHelpers.CreateMock();
        step1.When(s => s.Run(Arg.Any<CancellationToken>())).Do(_ => context.ExitCode = 1234);

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1], context: context);

        // Act
        var summary = await sut.RunPipeline(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(1234);
    }

    [Fact]
    public async Task RunPipeline_CustomExitCode_DoesNotOverrideAutomaticCode()
    {
        // Arrange
        var context = Substitute.For<IPipelineContext>();

        var step1 = PipelineStepHelpers.CreateMock();
        step1.When(s => s.Run(Arg.Any<CancellationToken>())).Do(_ => context.ExitCode = 1234);

        var step2 = PipelineStepHelpers.CreateMock();
        step2.Run(Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(steps: [step1, step2], context: context);

        // Act
        var summary = await sut.RunPipeline(CancellationToken.None);

        // Assert
        summary.ExitCode.ShouldBe(PipelineExitCodes.StoppedOnError);
    }

    [Fact]
    public async Task RunPipeline_WithPrePipelineHooks_RunsHooks()
    {
        // Arrange
        var hook1 = Substitute.For<IPrePipelineHook>();
        var hook2 = Substitute.For<IPrePipelineHook>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            prePipelineHooks: [hook1, hook2],
            steps: [step]
        );

        // Act
        await sut.RunPipeline(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            hook1.PrePipeline(Arg.Any<PrePipelineHookArgs>(), Arg.Any<CancellationToken>());
            hook2.PrePipeline(Arg.Any<PrePipelineHookArgs>(), Arg.Any<CancellationToken>());
            step.Run(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_WithPostPipelineHooks_RunsHooks()
    {
        // Arrange
        var hook1 = Substitute.For<IPostPipelineHook>();
        var hook2 = Substitute.For<IPostPipelineHook>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            steps: [step],
            postPipelineHooks: [hook1, hook2]
        );

        // Act
        await sut.RunPipeline(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            step.Run(Arg.Any<CancellationToken>());
            hook1.PostPipeline(Arg.Any<PostPipelineHookArgs>(), Arg.Any<CancellationToken>());
            hook2.PostPipeline(Arg.Any<PostPipelineHookArgs>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_PrePipelineHookError_StillRunsAllHooks()
    {
        // Arrange
        var hook1 = Substitute.For<IPrePipelineHook>();
        hook1.PrePipeline(Arg.Any<PrePipelineHookArgs>(), Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();

        var hook2 = Substitute.For<IPrePipelineHook>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            prePipelineHooks: [hook1, hook2],
            steps: [step]
        );

        // Act
        var act = () => sut.RunPipeline(CancellationToken.None);

        // Assert
        await act.ShouldNotThrowAsync();
        Received.InOrder(() =>
        {
            hook1.PrePipeline(Arg.Any<PrePipelineHookArgs>(), Arg.Any<CancellationToken>());
            hook2.PrePipeline(Arg.Any<PrePipelineHookArgs>(), Arg.Any<CancellationToken>());
            step.Run(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RunPipeline_PostPipelineHookError_StillRunsAllHooks()
    {
        // Arrange
        var hook1 = Substitute.For<IPostPipelineHook>();
        hook1.PostPipeline(Arg.Any<PostPipelineHookArgs>(), Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();

        var hook2 = Substitute.For<IPostPipelineHook>();
        var step = PipelineStepHelpers.CreateMock();

        var sut = DefaultPipelineRunnerHelpers.CreateRunner(
            steps: [step],
            postPipelineHooks: [hook1, hook2]
        );

        // Act
        var act = () => sut.RunPipeline(CancellationToken.None);

        // Assert
        await act.ShouldNotThrowAsync();
        Received.InOrder(() =>
        {
            step.Run(Arg.Any<CancellationToken>());
            hook1.PostPipeline(Arg.Any<PostPipelineHookArgs>(), Arg.Any<CancellationToken>());
            hook2.PostPipeline(Arg.Any<PostPipelineHookArgs>(), Arg.Any<CancellationToken>());
        });
    }
}
