using Ritten.Contracts;
using Ritten.Core;

namespace Ritten.Tests.Core;

public class RittenApplicationTests
{
    [Fact]
    public async Task Run_WithPassingStep_ReturnsZero()
    {
        // Act
        var exitCode = await RittenApplication.Run<TestPipeline>(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Run_WithCustomExitCode_ReturnsExitCode()
    {
        // Act
        var exitCode = await RittenApplication.Run<ExitCodePipeline>(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1234);
    }
}

class TestPipeline : Pipeline
{
    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.UseStep<TestPipelineStep>();
    }
}

class ExitCodePipeline : Pipeline
{
    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.UseStep<SetExitCodeTestPipelineStep>();
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
