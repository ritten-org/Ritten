using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Runs;

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
        exitCode.ShouldBe(ExitCode.Success);
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
        exitCode.ShouldBe(ExitCode.Failed);
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
    public void Build_SuppliesTheRunFactDefaultsWhenNothingElseDoes()
    {
        // The engine's defaults keep ValidateOnBuild happy on runtimes that know nothing about
        // these facts: steps see "not a pull request" and "no labels", never a missing
        // registration.
        var builder = WorkflowRunBuilderHelpers.Create();
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());

        var result = builder.Build(new TestJob());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
        builder.Services.Single(d => d.ServiceType == typeof(RunContext)).ImplementationInstance
            .ShouldBeOfType<RunContext>().Title.ShouldBe("Workflow");
        builder.Services.Single(d => d.ServiceType == typeof(PullRequest)).ImplementationInstance
            .ShouldBeOfType<PullRequest>().IsPullRequest.ShouldBeFalse();
        builder.Services.ShouldContain(d =>
            d.ServiceType == typeof(IPullRequestLabels) && d.ImplementationType == typeof(NoPullRequestLabels));
    }

    [Fact]
    public void Build_LeavesALabelReadTheHostRegistered()
    {
        // The default is only for runs where nobody knows better; anything the host or a runtime
        // declared wins over it.
        var own = Substitute.For<IPullRequestLabels>();
        var builder = WorkflowRunBuilderHelpers.Create();
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.Services.AddSingleton(own);

        var result = builder.Build(new TestJob());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
        builder.Services.Where(d => d.ServiceType == typeof(IPullRequestLabels))
            .ShouldHaveSingleItem().ImplementationInstance.ShouldBeSameAs(own);
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

sealed class StubRuntime : Runtime
{
    public string? SeenSecret { get; private set; }

    public override string Name => "stub";

    public override IReadOnlyCollection<string> Markers { get; } = ["STUB_CI"];

    public override IReadOnlyCollection<string> Claims { get; } = ["STUB_CI", "STUB_SECRET"];

    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment) =>
        SeenSecret = environment("STUB_SECRET");
}
