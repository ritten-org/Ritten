using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Pipelines.DotNetPackage;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The package pipeline is declared identically to the tool pipeline today; these tests pin the
/// wiring (jobs build, requirements hold) so the two can diverge deliberately rather than by rot.
/// </summary>
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
        var builder = PipelineHostBuilderHelpers.Create(pipeline.Label);
        var declared = (Job)pipeline.Jobs.Single(j => j.Name == job);
        return builder.Build(declared, settings);
    }
}
