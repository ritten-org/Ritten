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
    [Theory]
    [InlineData("verify")]
    [InlineData("build")]
    [InlineData("deploy")]
    public void EveryJobTheCliOffers_IsDeclared(string job)
    {
        Configure(job, new DotNetPackageSettings()).JobFound.ShouldBeTrue();
    }

    [Fact]
    public void AnUnknownJob_IsNotFound()
    {
        Configure("publish", new DotNetPackageSettings()).JobFound.ShouldBeFalse();
    }

    [Fact]
    public void Verify_DoesNotRequireAProject()
    {
        Configure("verify", new DotNetPackageSettings()).Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("build")]
    [InlineData("deploy")]
    public void ShippingJobs_RequireAProject(string job)
    {
        var errors = Configure(job, new DotNetPackageSettings()).Errors;

        errors.ShouldHaveSingleItem().ShouldContain("'build.project'");
    }

    [Theory]
    [InlineData("build")]
    [InlineData("deploy")]
    public void ShippingJobs_AcceptSettingsWithAProject(string job)
    {
        var settings = new DotNetPackageSettings
        {
            Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj" }
        };

        Configure(job, settings).Errors.ShouldBeEmpty();
    }

    private static PipelineHostBuilder Configure(string job, DotNetPackageSettings settings)
    {
        var pipeline = new DotNetPackagePipeline();
        var builder = PipelineHostBuilderHelpers.Create(job, pipeline.Name);
        pipeline.Configure(builder, settings);
        return builder;
    }
}
