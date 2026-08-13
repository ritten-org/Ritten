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
}

class TestPipeline : Pipeline
{
    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.UseStep<TestPipelineStep>();
    }
}

class TestPipelineStep : IPipelineStep
{
    public Task Run(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
