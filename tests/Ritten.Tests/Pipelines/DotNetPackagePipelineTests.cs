using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Pipelines;

public class DotNetPackagePipelineTests
{
    private static readonly DotNetPackageSettings Complete = new()
    {
        Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj" }
    };

    [Theory]
    [InlineData("status")]
    [InlineData("build")]
    [InlineData("check")]
    [InlineData("deploy")]
    public void EveryJobTheCliOffers_Builds(string job)
    {
        // Also exercises ValidateOnBuild, so a job whose steps have an unsatisfiable dependency fails here.
        var result = Build(job, Complete);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Fact]
    public void Build_DoesNotRequireAProject()
    {
        var result = Build("build", new DotNetPackageSettings());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Theory]
    [InlineData("status")]
    [InlineData("check")]
    [InlineData("deploy")]
    public void ShippingJobs_RequireAProject(string job)
    {
        var result = Build(job, new DotNetPackageSettings());

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("'build.project'");
    }

    private static Result<PipelineHost> Build(string job, DotNetPackageSettings settings)
    {
        var pipeline = new DotNetPackagePipeline();
        var builder = PipelineHostBuilderHelpers.Create(pipeline.Name);
        pipeline.Configure(builder, settings);
        return builder.Build(job);
    }
}
