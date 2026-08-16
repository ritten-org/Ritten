using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Pipelines.DotNetTool;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The .NET tool pipeline's jobs read the same project settings but don't share requirements:
/// only the jobs that ship a package need to know which project to pack.
/// </summary>
public class DotNetToolPipelineTests
{
    private static readonly DotNetToolSettings Complete = new()
    {
        Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj" }
    };

    [Fact]
    public void DeclaresTheJobsTheCliOffers()
    {
        new DotNetToolPipeline().Jobs.Select(j => j.Name).ShouldBe(["status", "build", "check", "deploy"]);
    }

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
        var result = Build("build", new DotNetToolSettings());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Theory]
    [InlineData("status")]
    [InlineData("check")]
    [InlineData("deploy")]
    public void ShippingJobs_RequireAProject(string job)
    {
        var result = Build(job, new DotNetToolSettings());

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("'build.project'");
    }

    [Theory]
    [InlineData("check")]
    [InlineData("deploy")]
    public void ShippingJobs_AcceptSettingsWithAProject(string job)
    {
        var result = Build(job, Complete);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Fact]
    public void Deploy_BuildsWithoutAnyEnvironment()
    {
        // Credentials are resolved by the steps that use them, after the gates — so an offline
        // deploy composes, and one that's at rest exits 0 without ever needing them.
        var result = Build("deploy", Complete, environment: PipelineHostBuilderHelpers.Empty);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    private static Result<PipelineHost> Build(
        string job,
        DotNetToolSettings settings,
        Func<string, string?>? environment = null,
        bool dryRun = false)
    {
        var pipeline = new DotNetToolPipeline();
        var builder = PipelineHostBuilderHelpers.Create(pipeline.Label, environment, dryRun);
        var declared = (Job)pipeline.Jobs.Single(j => j.Name == job);
        return builder.Build(declared, settings);
    }
}
