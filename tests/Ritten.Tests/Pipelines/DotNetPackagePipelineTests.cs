using Ritten.Core;
using Ritten.Pipelines.DotNet;
using Ritten.Tests.Core.Helpers;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The .NET package pipeline's jobs read the same project settings but don't share requirements:
/// only the jobs that ship a package need to know which project to pack.
/// </summary>
public class DotNetPackagePipelineTests
{
    private static readonly DotNetPackageSettings Complete = new()
    {
        Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj" }
    };

    [Theory]
    [InlineData("verify")]
    [InlineData("build")]
    [InlineData("deploy")]
    public void EveryJobTheCliOffers_Builds(string job)
    {
        // Also exercises ValidateOnBuild, so a job whose steps have an unsatisfiable dependency fails here.
        var result = Build(job, Complete);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Fact]
    public void AnUnknownJob_IsRejected()
    {
        var result = Build("publish", Complete);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("no job named 'publish'");
    }

    [Fact]
    public void Verify_DoesNotRequireAProject()
    {
        var result = Build("verify", new DotNetPackageSettings());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Theory]
    [InlineData("build")]
    [InlineData("deploy")]
    public void ShippingJobs_RequireAProject(string job)
    {
        var result = Build(job, new DotNetPackageSettings());

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("'build.project'");
    }

    [Theory]
    [InlineData("build")]
    [InlineData("deploy")]
    public void ShippingJobs_AcceptSettingsWithAProject(string job)
    {
        var result = Build(job, Complete);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    private static Result<PipelineHost> Build(string job, DotNetPackageSettings settings)
    {
        var pipeline = new DotNetPackagePipeline();
        var builder = PipelineHostBuilderHelpers.Create(pipeline.Name);
        pipeline.Configure(builder, settings);
        return builder.Build(job);
    }
}
