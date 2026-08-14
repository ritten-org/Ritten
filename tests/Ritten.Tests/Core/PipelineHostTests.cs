using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
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
        // Arrange
        var builder = PipelineHostBuilderHelpers.Create("verify");

        // Act — two jobs declared, only one requested.
        builder.AddJob("verify", job => job.UseStep<FirstStep>());
        builder.AddJob("deploy", job => job.UseStep<SecondStep>());

        // Assert
        builder.JobFound.ShouldBeTrue();
        builder.Services.Count(d => d.ServiceType == typeof(IPipelineStep)).ShouldBe(1);
        builder.Services.ShouldContain(d => d.ImplementationType == typeof(FirstStep));
        builder.Services.ShouldNotContain(d => d.ImplementationType == typeof(SecondStep));
    }

    private static PipelineHost BuildHost(IPipelineStep step)
    {
        var builder = PipelineHostBuilderHelpers.Create("verify");
        builder.AddJob("verify", _ => { });
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());
        builder.Services.AddSingleton(step);

        return builder.Build();
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
