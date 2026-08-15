using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Pipelines;
using Ritten.Tests.Core.Helpers;

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
        builder.Services.ShouldContain(d => d.ServiceType == typeof(FirstStep));
        builder.Services.ShouldNotContain(d => d.ServiceType == typeof(SecondStep));
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
            .Requires(settings.Repository));

        // Act
        var result = builder.Build("deploy");

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Select(e => e.Message).ShouldBe([
            "'build.project' not set in ritten.json.",
            "'repository' not set in ritten.json."
        ]);
    }

    [Fact]
    public void Build_RejectsAStepWhoseInputNothingProduces()
    {
        // A consuming step before its producer is a composition mistake, caught before any work.
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", job => job.UseStep<ProjectConsumingStep>());
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());

        var result = builder.Build("verify");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message
            .ShouldContain("no earlier step produces");
    }

    [Fact]
    public void Build_RejectsAStepWithoutAStepAttribute()
    {
        // Name and kind are required, not defaulted: an unclassified step is a mistake, not work.
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", job => job.UseStep<UnclassifiedStep>());

        var result = builder.Build("verify");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("[Step]");
    }

    [Fact]
    public void Build_RejectsAJobThatPublishesWithoutAGate()
    {
        // The rule reports rather than repairs, so the job's declaration stays the whole truth.
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("deploy", job => job.UseStep<PublishingStep>());
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());

        var result = builder.Build("deploy");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("gate");
    }

    [Fact]
    public void Build_RunsRulesThePipelineRegisters()
    {
        var rule = Substitute.For<IJobRule>();
        rule.Check(Arg.Any<IReadOnlyList<JobStep>>()).Returns([new Error("House rule broken.")]);
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", job => job.UseStep<FirstStep>());
        builder.Services.AddSingleton(rule);
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());

        var result = builder.Build("verify");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("House rule broken.");
    }

    [Fact]
    public void Build_AcceptsAConsumerDeclaredAfterItsProducer()
    {
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", job => job
            .UseStep<ProjectProducingStep>()
            .UseStep<ProjectConsumingStep>());
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());

        var result = builder.Build("verify");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    private static (PipelineHost Host, StepProbe Probe) BuildHost<TStep>() where TStep : class
    {
        var probe = new StepProbe();
        var builder = PipelineHostBuilderHelpers.Create();
        builder.AddJob("verify", job => job.UseStep<TStep>());
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());
        builder.Services.AddSingleton(probe);

        var result = builder.Build("verify");
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

[Step("second", StepKind.Work)]
class SecondStep
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
