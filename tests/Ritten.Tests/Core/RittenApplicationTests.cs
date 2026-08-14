using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Core;

public class RittenApplicationTests
{
    [Fact]
    public async Task Run_WithPassingStep_ReturnsZero()
    {
        // Act
        var exitCode = await RittenApplication.Run<TestPipeline>(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.Success);
    }

    [Fact]
    public async Task Run_WithInvalidConfiguration_ReturnsConfigurationErrorBeforeRunningAnySteps()
    {
        // Arrange
        var log = Substitute.For<IPipelineLog>();
        var step = PipelineStepHelpers.CreateMock();

        using var app = BuildApplication(log, step, services => services
            .AddOptions<ProjectSettings>()
            .Configure(o => o.ProjectFile = "")
            .Validate(o => !string.IsNullOrEmpty(o.ProjectFile), "DotNet:ProjectFile must be set.")
            .ValidateOnStart());

        // Act
        var exitCode = await app.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.ConfigurationError);
        await step.DidNotReceive().Run(Arg.Any<CancellationToken>());
        log.Received().Log(PipelineLogLevel.Error, Arg.Is<string>(m => m.Contains("DotNet:ProjectFile must be set.")));
    }

    [Fact]
    public async Task Run_WithSeveralInvalidOptionsTypes_ReportsEveryFailure()
    {
        // Arrange
        var log = Substitute.For<IPipelineLog>();

        using var app = BuildApplication(log, PipelineStepHelpers.CreateMock(), services =>
        {
            services.AddOptions<ProjectSettings>()
                .Configure(o => o.ProjectFile = "")
                .Validate(o => !string.IsNullOrEmpty(o.ProjectFile), "DotNet:ProjectFile must be set.")
                .ValidateOnStart();

            services.AddOptions<FeedSettings>()
                .Configure(o => o.Feed = "")
                .Validate(o => !string.IsNullOrEmpty(o.Feed), "NuGet:Feed must be set.")
                .ValidateOnStart();
        });

        // Act
        var exitCode = await app.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.ConfigurationError);
        log.Received().Log(PipelineLogLevel.Error, Arg.Is<string>(m => m.Contains("DotNet:ProjectFile must be set.")));
        log.Received().Log(PipelineLogLevel.Error, Arg.Is<string>(m => m.Contains("NuGet:Feed must be set.")));
    }

    [Fact]
    public async Task Run_WithValidConfiguration_RunsTheSteps()
    {
        // Arrange
        var step = PipelineStepHelpers.CreateMock();

        using var app = BuildApplication(Substitute.For<IPipelineLog>(), step, services => services
            .AddOptions<ProjectSettings>()
            .Configure(o => o.ProjectFile = "src/Thing/Thing.csproj")
            .Validate(o => !string.IsNullOrEmpty(o.ProjectFile), "DotNet:ProjectFile must be set.")
            .ValidateOnStart());

        // Act
        var exitCode = await app.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.Success);
        await step.Received().Run(Arg.Any<CancellationToken>());
    }

    private static RittenApplication BuildApplication(IPipelineLog log, IPipelineStep step, Action<IServiceCollection> configure)
    {
        var builder = new RittenApplicationBuilder();

        // Registered ahead of Build(), which only supplies the Spectre reporter if nothing else has.
        builder.Services.AddSingleton(log);
        builder.Services.AddSingleton(step);
        builder.Services.AddSingleton<Pipeline>(new TestPipeline());
        configure(builder.Services);

        return builder.Build();
    }

    private class ProjectSettings
    {
        public string ProjectFile { get; set; } = "";
    }

    private class FeedSettings
    {
        public string Feed { get; set; } = "";
    }
}

class TestPipeline : Pipeline
{
    /// <inheritdoc />
    public override string Name => "Test";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.UseStep<TestPipelineStep>();
    }
}

class TestPipelineStep : IPipelineStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StepResult.Successful);
    }
}
