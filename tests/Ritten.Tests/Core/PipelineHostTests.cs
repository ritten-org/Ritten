using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Pipelines.DotNet;
using Ritten.Tests.Core.Helpers;

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

    [Fact]
    public void Build_RegistersOnlyTheRequestedJobsSteps()
    {
        // Arrange — two jobs declared, only one requested.
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", job => job.UseStep<FirstStep>());
        builder.AddJob("deploy", job => job.UseStep<SecondStep>());

        // Act
        var result = builder.Build("verify");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
        builder.Services.Count(d => d.ServiceType == typeof(IPipelineStep)).ShouldBe(1);
        builder.Services.ShouldContain(d => d.ImplementationType == typeof(FirstStep));
        builder.Services.ShouldNotContain(d => d.ImplementationType == typeof(SecondStep));
    }

    [Fact]
    public void Build_ReportsEveryUnmetRequirementAtOnce()
    {
        // Arrange — being told about all of them beats fixing them one run at a time. The keys are
        // inferred from the expressions, so they can't drift from the properties they describe.
        var settings = new DotNetToolSettings();
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("deploy", job => job
            .Requires(settings.Build.Project)
            .Requires(settings.Changelog.Repository));

        // Act
        var result = builder.Build("deploy");

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Select(e => e.Message).ShouldBe([
            "'build.project' not set in ritten.json.",
            "'changelog.repository' not set in ritten.json."
        ]);
    }

    private static PipelineHost BuildHost(IPipelineStep step)
    {
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", _ => { });
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());
        builder.Services.AddSingleton(step);

        var result = builder.Build("verify");
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}

class FirstStep : IPipelineStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default) =>
        Task.FromResult(StepResult.Successful);
}

class SecondStep : IPipelineStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default) =>
        Task.FromResult(StepResult.Successful);
}
