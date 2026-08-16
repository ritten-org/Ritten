using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Pipelines;
using Ritten.Tests.Core.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Core;

public class PipelineHostTests
{
    [Fact]
    public async Task Run_WithPassingStep_ReturnsZero()
    {
        // Arrange
        var (host, probe) = BuildHost<ProbeStep>();
        using var _ = host;

        // Act
        var exitCode = await host.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_WithFailingStep_ReturnsFailure()
    {
        // Arrange
        var (host, _) = BuildHost<FailingStep>();
        using var _1 = host;

        // Act
        var exitCode = await host.Run(TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(PipelineExitCodes.Failed);
    }

    [Fact]
    public void Build_ReportsEveryUnmetRequirementAtOnce()
    {
        // Arrange — being told about all of them beats fixing them one run at a time. The keys are
        // derived from the property chains, so they can't drift from the settings they describe.
        var job = new TestJob("deploy", validate: s => s.Require(x => x.Build.Project).Require(x => x.Repository));
        var builder = PipelineHostBuilderHelpers.Create();

        // Act
        var result = builder.Build(job, new DotNetToolSettings());

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Select(e => e.Message).ShouldBe([
            "'build.project' not set in ritten.json.",
            "'repository' not set in ritten.json."
        ]);
    }

    [Fact]
    public void Build_RejectsAStepWithoutAStepAttribute()
    {
        // Name and kind are required, not defaulted: an unclassified step is a mistake, not work.
        var job = new TestJob(steps: [typeof(UnclassifiedStep)]);
        var builder = PipelineHostBuilderHelpers.Create();

        var result = builder.Build(job, new DotNetToolSettings());

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("[Step]");
    }

    [Fact]
    public void Build_RunsRulesThePipelineRegisters()
    {
        var rule = Substitute.For<IJobRule>();
        rule.Check(Arg.Any<IJob>()).Returns([new Error("House rule broken.")]);
        var job = new TestJob(steps: [typeof(FirstStep)]);
        var builder = PipelineHostBuilderHelpers.Create();
        builder.Services.AddSingleton(rule);
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());

        var result = builder.Build(job, new DotNetToolSettings());

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("House rule broken.");
    }

    private static (PipelineHost Host, StepProbe Probe) BuildHost<TStep>() where TStep : class
    {
        var probe = new StepProbe();
        var job = new TestJob(steps: [typeof(TStep)]);
        var builder = PipelineHostBuilderHelpers.Create();
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());
        builder.Services.AddSingleton(probe);

        var result = builder.Build(job, new DotNetToolSettings());
        result.IsSuccess.ShouldBeTrue();
        return (result.Value, probe);
    }
}

public sealed class StepProbe
{
    public List<string> Ran { get; } = [];
}

[Step("probe", StepKind.Work)]
class ProbeStep(StepProbe probe)
{
    public Task<StepResult> Run(CancellationToken cancellationToken)
    {
        probe.Ran.Add(GetType().Name);
        return Task.FromResult(StepResult.Successful);
    }
}

[Step("failing", StepKind.Work)]
class FailingStep
{
    // Synchronous on purpose: the failing-step test also covers the sync convention end to end.
    public StepResult Run() => StepResult.Failed("Nope.");
}

[Step("first", StepKind.Work)]
class FirstStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default) =>
        Task.FromResult(StepResult.Successful);
}

class UnclassifiedStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}

[Step("publisher", StepKind.Publish)]
class PublishingStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}

[Step("producer", StepKind.Work)]
class ProjectProducingStep
{
    public Task<StepResult<Project>> Run(CancellationToken cancellationToken) =>
        Task.FromResult<StepResult<Project>>(new Project { Name = "Thing", Version = NuGetVersion.Parse("1.0.0") });
}

[Step("consumer", StepKind.Work)]
class ProjectConsumingStep
{
    public Task<StepResult> Run(Project project, CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}
