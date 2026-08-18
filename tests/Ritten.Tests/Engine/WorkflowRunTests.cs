using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.DotNet;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine;

public class WorkflowRunTests
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
        exitCode.ShouldBe(WorkflowExitCodes.Success);
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
        exitCode.ShouldBe(WorkflowExitCodes.Failed);
    }

    [Fact]
    public void Build_ReportsEveryUnmetRequirementAtOnce()
    {
        // Arrange — being told about all of them beats fixing them one run at a time. The keys are
        // derived from the property chains, so they can't drift from the settings they describe.
        var job = new TestJob("deploy", validate: s => s.Require(x => x.Build.Project).Require(x => x.Repository));
        var builder = WorkflowRunBuilderHelpers.Create();

        // Act
        var result = builder.Build(job);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Select(e => e.Message).ShouldBe([
            "'build.project' not set in ritten.json.",
            "'repository' not set in ritten.json."
        ]);
    }

    [Fact]
    public void Build_NamesTheHostsProjectFileInSettingsErrors()
    {
        // The error points at the file the reader actually has, whatever the host called it.
        var job = new TestJob("deploy", validate: s => s.Require(x => x.Build.Project));
        var builder = WorkflowRunBuilderHelpers.Create(fileName: "build.json");

        var result = builder.Build(job);

        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("'build.project' not set in build.json.");
    }

    [Fact]
    public void Build_RunsRulesTheWorkflowRegisters()
    {
        var rule = Substitute.For<IJobRule>();
        rule.Check(Arg.Any<IJob>()).Returns([new Error("House rule broken.")]);
        var job = new TestJob(steps: [Step.FromType<FirstStep>()]);
        var builder = WorkflowRunBuilderHelpers.Create();
        builder.Services.AddSingleton(rule);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());

        var result = builder.Build(job);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("House rule broken.");
    }

    [Fact]
    public void Build_ConfiguresTheServicesOfTheDetectedRuntime()
    {
        var runtime = new StubRuntime();
        var builder = WorkflowRunBuilderHelpers.Create(runtimes: new RuntimeRegistry().Add(runtime));
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());

        var result = builder.Build(new TestJob());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
        // The runtime reads its claimed variables from the unfiltered environment: they're its own.
        runtime.SeenSecret.ShouldBe("set");
    }

    [Fact]
    public void Build_HidesClaimedVariablesFromSettingsValidation()
    {
        // The variable exists in the process environment, but the runtime consumed it — so a job
        // requiring it fails loudly instead of running with a value that belongs to the runtime.
        var job = new TestJob(validate: s => s.RequireEnvironment("STUB_SECRET"));
        var builder = WorkflowRunBuilderHelpers.Create(runtimes: new RuntimeRegistry().Add(new StubRuntime()));

        var result = builder.Build(job);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("STUB_SECRET is not set.");
    }

    private static (WorkflowRun Host, StepProbe Probe) BuildHost<TStep>() where TStep : class
    {
        var probe = new StepProbe();
        var job = new TestJob(steps: [Step.FromType<TStep>()]);
        var builder = WorkflowRunBuilderHelpers.Create();
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.Services.AddSingleton(probe);

        var result = builder.Build(job);
        result.IsSuccess.ShouldBeTrue();
        return (result.Value, probe);
    }
}

public sealed class StepProbe
{
    public List<string> Ran { get; } = [];
}

sealed class StubRuntime : Runtime
{
    public string? SeenSecret { get; private set; }

    public override string Name => "stub";

    public override IReadOnlyCollection<string> Markers { get; } = ["STUB_CI"];

    public override IReadOnlyCollection<string> Claims { get; } = ["STUB_CI", "STUB_SECRET"];

    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment) =>
        SeenSecret = environment("STUB_SECRET");
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
