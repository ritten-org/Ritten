using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Reporting;
using Ritten.Tests.Core.Helpers;
using Spectre.Console;

namespace Ritten.Tests.Core;

public class PipelineHostTests
{
    [Fact]
    public async Task Run_WithPassingStep_ReturnsZero()
    {
        // Arrange
        var step = PipelineStepHelpers.CreateMock();
        using var host = BuildHost(step);

        // Act
        var exitCode = await host.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.Success);
        await step.Received().Run(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_WithFailingStep_ReturnsFailure()
    {
        // Arrange
        var step = PipelineStepHelpers.CreateMock();
        step.Run(Arg.Any<CancellationToken>()).Returns(StepResult.Failed("Nope."));
        using var host = BuildHost(step);

        // Act
        var exitCode = await host.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.Failed);
    }

    private static PipelineHost BuildHost(IPipelineStep step)
    {
        var project = new RittenProject
        {
            Directory = Path.GetTempPath(),
            Settings = JsonSerializer.Deserialize<JsonElement>("{}")
        };

        var builder = new PipelineHostBuilder(project, new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail));
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());
        builder.Services.AddSingleton(step);
        builder.Services.AddSingleton<Pipeline>(new TestPipeline());

        return builder.Build();
    }
}

class TestSettings;

class TestPipeline : Pipeline<TestSettings>
{
    /// <inheritdoc />
    public override string Name => "Test";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder, TestSettings settings) =>
        builder.UseStep<TestPipelineStep>();
}

class TestPipelineStep : IPipelineStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default) =>
        Task.FromResult(StepResult.Successful);
}
