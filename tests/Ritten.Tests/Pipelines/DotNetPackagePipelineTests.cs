using Ritten.Contracts;
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

    [Fact]
    public void Deploy_RefusesUpFrontWithoutTheCredentialsItNeeds()
    {
        // Before anything runs, rather than after a tag has already been pushed.
        var result = Build("deploy", Complete, environment: PipelineHostBuilderHelpers.Empty);

        result.IsError.ShouldBeTrue();
        result.Errors.Select(e => e.Message).ShouldBe([
            "RITTEN_NUGET_API_KEY is not set.",
            "GITHUB_REPOSITORY_ID is not set."
        ]);
    }

    [Fact]
    public void Deploy_NeedsNoCredentialsToRehearse()
    {
        // A dry run stands in for the clients that would have used them.
        var result = Build("deploy", Complete, environment: PipelineHostBuilderHelpers.Empty, dryRun: true);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Fact]
    public void Deploy_WarnsWhenARehearsalWouldPassButTheRealRunWouldNot()
    {
        // A rehearsal that passes where the real thing fails is worse than no rehearsal.
        var log = Substitute.For<IPipelineLog>();
        var builder = PipelineHostBuilderHelpers.Create(
            log: log, environment: PipelineHostBuilderHelpers.Empty, dryRun: true);

        new DotNetPackagePipeline().Configure(builder, Complete);
        builder.Build("deploy").Value.ShouldNotBeNull().Dispose();

        log.Received().Log(
            PipelineLogLevel.Warning,
            Arg.Is<string>(m => m.Contains("RITTEN_NUGET_API_KEY")),
            Arg.Any<Exception>());
    }

    private static Result<PipelineHost> Build(
        string job,
        DotNetPackageSettings settings,
        Func<string, string?>? environment = null,
        bool dryRun = false)
    {
        var pipeline = new DotNetPackagePipeline();
        var builder = PipelineHostBuilderHelpers.Create(pipeline.Name, environment, dryRun);
        pipeline.Configure(builder, settings);
        return builder.Build(job);
    }
}
