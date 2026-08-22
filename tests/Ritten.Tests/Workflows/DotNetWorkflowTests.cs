using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Tests.Engine.Helpers;
using Ritten.Workflows.DotNet;

namespace Ritten.Tests.Workflows;

/// <summary>
/// The dotnet workflow ships nothing, so it offers no status or deploy and none of its jobs need
/// any configuration. These tests pin that shape.
/// </summary>
public class DotNetWorkflowTests
{
    [Fact]
    public void OffersInitBuildAndCheck()
    {
        new DotNetWorkflow().Jobs.Select(j => j.Name).ShouldBe(["init", "build", "check"]);
    }

    [Theory]
    [InlineData("init")]
    [InlineData("build")]
    [InlineData("check")]
    public void EveryJobTheCliOffers_BuildsWithoutAnySettings(string job)
    {
        // Also exercises ValidateOnBuild, so a job whose steps have an unsatisfiable dependency fails here.
        var result = Build(job, "{}");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    private static Result<WorkflowRun> Build(string job, string settings)
    {
        var workflow = new DotNetWorkflow();
        var builder = WorkflowRunBuilderHelpers.Create(workflow.Label, settings: settings);
        return builder.Build(workflow.Jobs.Single(j => j.Name == job));
    }
}
