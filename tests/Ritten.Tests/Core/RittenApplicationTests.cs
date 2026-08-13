using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Core.Extensions;
using Ritten.Core.Steps;

namespace Ritten.Tests.Core;

public class RittenApplicationTests
{
    [Fact]
    public async Task RunWithExitCodeAsync_EmptyApplication_DoesNotThrowOrHang()
    {
        // Arrange
        var builder = RittenApplication.CreateBuilder();
        builder.Services.AddStep<TestPipelineStep>();

        var pipeline = builder.Build();

        pipeline.UseStep<TestPipelineStep>();

        // Act
        var exitCode = await pipeline.RunWithExitCodeAsync(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task RunWithExitCodeAsync_WithCustomExitCode_ReturnsExitCode()
    {
        // Arrange
        var builder = RittenApplication.CreateBuilder();
        builder.Services.AddStep<SetExitCodeTestPipelineStep>();

        var pipeline = builder.Build();

        pipeline.UseStep<SetExitCodeTestPipelineStep>();

        // Act
        var exitCode = await pipeline.RunWithExitCodeAsync(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1234);
    }

    [Fact]
    public async Task StartAsync_MultipleTimes_ThrowsOnSecondCall()
    {
        // Arrange
        var builder = RittenApplication.CreateBuilder();
        builder.Services.AddStep<TestPipelineStep>();

        var pipeline = builder.Build();

        // Act
        await pipeline.StartAsync(TestContext.Current.CancellationToken);
        var act = () => pipeline.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void UseStep_AddsStepToCollector()
    {
        // Arrange
        var collector = Substitute.For<IPipelineStepCollection>();

        var builder = RittenApplication.CreateBuilder();
        builder.Services.AddSingleton(collector);

        var pipeline = builder.Build();

        // Act
        pipeline.UseStep(typeof(TestPipelineStep));

        // Assert
        collector.Received().AddStep(typeof(TestPipelineStep));
    }

    [Fact]
    public void UseStepGeneric_AddsStepToCollector()
    {
        // Arrange
        var collector = Substitute.For<IPipelineStepCollection>();

        var builder = RittenApplication.CreateBuilder();
        builder.Services.AddSingleton(collector);

        var pipeline = builder.Build();

        // Act
        pipeline.UseStep<TestPipelineStep>();

        // Assert
        collector.Received().AddStep(typeof(TestPipelineStep));
    }
}

class TestPipelineStep : IPipelineStep
{
    public Task Run(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

class SetExitCodeTestPipelineStep(IPipelineContext context) : IPipelineStep
{
    public Task Run(CancellationToken cancellationToken = default)
    {
        context.ExitCode = 1234;
        return Task.CompletedTask;
    }
}
