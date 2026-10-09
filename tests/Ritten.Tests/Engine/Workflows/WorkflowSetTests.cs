using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Tests.Support;
using Ritten.Workflows.DotNetPackage;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Engine.Workflows;

public class WorkflowSetTests
{
    [Fact]
    public void Workflows_TakesEachWorkflowsDeclaredJobs()
    {
        var registry = new WorkflowSet([new DotNetToolWorkflow(), new DotNetPackageWorkflow()]);

        registry.Workflows.Select(p => p.Name).ShouldBe(["dotnet-tool", "dotnet-package"]);
        registry.Find("dotnet-tool").ShouldNotBeNull().Jobs.Select(j => j.Name).ShouldBe(["init", "status", "build", "install", "prepare", "check", "deploy"]);
    }

    [Fact]
    public void Find_MatchesTheNameCaseInsensitively()
    {
        // ritten.json is a hand-edited file.
        var registry = new WorkflowSet([new DotNetToolWorkflow()]);

        registry.Find("DotNet-Tool").ShouldNotBeNull();
    }

    [Fact]
    public void Find_MissesAnUnregisteredName()
    {
        var registry = new WorkflowSet([new DotNetToolWorkflow()]);

        registry.Find("terraform").ShouldBeNull();
    }

    [Fact]
    public void Validate_PassesTheShippedWorkflows()
    {
        var registry = new WorkflowSet([new DotNetToolWorkflow(), new DotNetPackageWorkflow()]);

        registry.Validate([]).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReportsWorkflowsSharingAName()
    {
        var registry = new WorkflowSet([new FakeWorkflow("twin"), new FakeWorkflow("twin")]);

        registry.Validate([]).ShouldHaveSingleItem().Message.ShouldContain("'twin'");
    }

    [Fact]
    public void Validate_ReportsJobsSharingAName()
    {
        var registry = new WorkflowSet([new FakeWorkflow("fake",
            new TestJob(steps: [Step.FromType<FirstStep>()]),
            new TestJob(steps: [Step.FromType<FirstStep>()]))]);

        registry.Validate([]).ShouldHaveSingleItem().Message.ShouldContain("two jobs named 'verify'");
    }

    [Fact]
    public void Validate_ReportsAJobThatPublishesWithoutAGate()
    {
        // The purely structural job rules are judged on the declarations, so a bad shape fails
        // every run at startup, not just the run that selects it.
        var registry = new WorkflowSet([new FakeWorkflow("fake",
            new TestJob("deploy", steps: [Step.FromType<PublishingStep>()]))]);

        var error = registry.Validate([]).ShouldHaveSingleItem();
        error.Message.ShouldStartWith("fake deploy:");
        error.Message.ShouldContain("gate");
    }

    [Fact]
    public void Validate_ReportsAStepWhoseInputNothingProduces()
    {
        // Step parameters come only from state, so a consumer before its producer is judged on
        // the declarations — no container required.
        var registry = new WorkflowSet([new FakeWorkflow("fake",
            new TestJob(steps: [Step.FromType<ProjectConsumingStep>()]))]);

        registry.Validate([]).ShouldHaveSingleItem().Message.ShouldContain("no earlier step produces");
    }

    [Fact]
    public void Validate_AcceptsAConsumerDeclaredAfterItsProducer()
    {
        var registry = new WorkflowSet([new FakeWorkflow("fake",
            new TestJob(steps: [Step.FromType<ProjectProducingStep>(), Step.FromType<ProjectConsumingStep>()]))]);

        registry.Validate([]).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_CountsTheJobsArgumentsAsProducedBeforeTheFirstStep()
    {
        // A job's arguments are in the state before any step runs, so a step can read one first thing.
        var registry = new WorkflowSet([new FakeWorkflow("fake",
            new TestJob<ProjectArguments>(steps: [Step.FromType<ProjectConsumingStep>()]))]);

        registry.Validate([]).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_RunsTheRulesTheApplicationRegisters()
    {
        var rule = Substitute.For<IJobRule>();
        rule.Check(Arg.Any<IJob>()).Returns([new Error("House rule broken.")]);
        var registry = new WorkflowSet([new FakeWorkflow("fake", new TestJob())]);

        registry.Validate([rule]).ShouldHaveSingleItem().Message.ShouldBe("fake verify: House rule broken.");
    }

    private sealed record ProjectArguments(Project Project);

    private sealed class FakeWorkflow(string name, params IReadOnlyList<IJob> jobs) : IWorkflow
    {
        public string Name => name;

        public string Label => name;

        public IReadOnlyList<IJob> Jobs => jobs;
    }
}
