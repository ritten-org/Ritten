using Ritten.Core;
using Ritten.Pipelines.DotNetPackage;
using Ritten.Pipelines.DotNetTool;
using Ritten.Tests.Support;

namespace Ritten.Tests.Core;

public class PipelineRegistryTests
{
    [Fact]
    public void Add_TakesEachPipelinesDeclaredJobs()
    {
        var registry = new PipelineRegistry()
            .Add(new DotNetToolPipeline())
            .Add(new DotNetPackagePipeline());

        registry.Pipelines.Select(p => p.Name).ShouldBe(["dotnet-tool", "dotnet-package"]);
        registry.Find("dotnet-tool").ShouldNotBeNull().Jobs.Select(j => j.Name).ShouldBe(["status", "build", "check", "deploy"]);
    }

    [Fact]
    public void Find_MatchesTheNameCaseInsensitively()
    {
        // ritten.json is a hand-edited file.
        var registry = new PipelineRegistry().Add(new DotNetToolPipeline());

        registry.Find("DotNet-Tool").ShouldNotBeNull();
    }

    [Fact]
    public void Find_MissesAnUnregisteredName()
    {
        var registry = new PipelineRegistry().Add(new DotNetToolPipeline());

        registry.Find("terraform").ShouldBeNull();
    }

    [Fact]
    public void Validate_PassesTheShippedPipelines()
    {
        var registry = new PipelineRegistry()
            .Add(new DotNetToolPipeline())
            .Add(new DotNetPackagePipeline());

        registry.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReportsPipelinesSharingAName()
    {
        var registry = new PipelineRegistry()
            .Add(new FakePipeline("twin"))
            .Add(new FakePipeline("twin"));

        registry.Validate().ShouldHaveSingleItem().Message.ShouldContain("'twin'");
    }

    [Fact]
    public void Validate_ReportsJobsSharingAName()
    {
        var registry = new PipelineRegistry().Add(new FakePipeline("fake",
            new TestJob(steps: [typeof(FirstStep)]),
            new TestJob(steps: [typeof(FirstStep)])));

        registry.Validate().ShouldHaveSingleItem().Message.ShouldContain("two jobs named 'verify'");
    }

    [Fact]
    public void Validate_ReportsAJobThatPublishesWithoutAGate()
    {
        // The purely structural job rules are judged on the declarations, so a bad shape fails
        // every run at startup, not just the run that selects it.
        var registry = new PipelineRegistry().Add(new FakePipeline("fake",
            new TestJob("deploy", steps: [typeof(PublishingStep)])));

        var error = registry.Validate().ShouldHaveSingleItem();
        error.Message.ShouldStartWith("fake deploy:");
        error.Message.ShouldContain("gate");
    }

    [Fact]
    public void Validate_ReportsAStepWhoseInputNothingProduces()
    {
        // Step parameters come only from state, so a consumer before its producer is judged on
        // the declarations — no container required.
        var registry = new PipelineRegistry().Add(new FakePipeline("fake",
            new TestJob(steps: [typeof(ProjectConsumingStep)])));

        registry.Validate().ShouldHaveSingleItem().Message.ShouldContain("no earlier step produces");
    }

    [Fact]
    public void Validate_AcceptsAConsumerDeclaredAfterItsProducer()
    {
        var registry = new PipelineRegistry().Add(new FakePipeline("fake",
            new TestJob(steps: [typeof(ProjectProducingStep), typeof(ProjectConsumingStep)])));

        registry.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReportsAStepThatIsNotAStep()
    {
        var registry = new PipelineRegistry().Add(new FakePipeline("fake",
            new TestJob(steps: [typeof(UnclassifiedStep)])));

        registry.Validate().ShouldHaveSingleItem().Message.ShouldContain("[Step]");
    }

    private sealed class FakePipeline(string name, params IReadOnlyList<IJob> jobs) : IPipeline
    {
        public string Name => name;

        public string Label => name;

        public IReadOnlyList<IJob> Jobs => jobs;
    }
}
