using Ritten.Core;
using Ritten.Pipelines.DotNet;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The dotnet pipeline ships nothing, so it offers no status or deploy and none of its jobs need
/// any configuration. These tests pin that shape.
/// </summary>
public class DotNetPipelineTests
{
    [Fact]
    public void OffersOnlyBuildAndCheck()
    {
        new DotNetPipeline().Jobs.Select(j => j.Name).ShouldBe(["build", "check"]);
    }

    [Theory]
    [InlineData("build")]
    [InlineData("check")]
    public void EveryJobTheCliOffers_BuildsWithoutAnySettings(string job)
    {
        // Also exercises ValidateOnBuild, so a job whose steps have an unsatisfiable dependency fails here.
        var result = Build(job, "{}");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    private static Result<PipelineRun> Build(string job, string settings)
    {
        var pipeline = new DotNetPipeline();
        var builder = PipelineRunBuilderHelpers.Create(pipeline.Label, settings: settings);
        return builder.Build(pipeline.Jobs.Single(j => j.Name == job));
    }
}
