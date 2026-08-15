using Ritten.Contracts;
using Ritten.Core;
using Ritten.Pipelines;
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
    public void AnUnknownJob_IsRejected()
    {
        var result = Build("publish", Complete);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("no job named 'publish'");
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

        new DotNetToolPipeline().Configure(builder, Complete);
        builder.Build("deploy").Value.ShouldNotBeNull().Dispose();

        log.Received().Log(
            PipelineLogLevel.Warning,
            Arg.Is<string>(m => m.Contains("RITTEN_NUGET_API_KEY")),
            Arg.Any<Exception>());
    }

    [Fact]
    public void CoverageStepsJoinTheJobsOnlyWhenConfigured()
    {
        // The job's declaration matches what actually runs: no configuration, no steps.
        var withCoverage = Build("check", Complete with { Coverage = new CoverageSettings() });
        var withoutCoverage = Build("check", Complete);

        withCoverage.IsSuccess.ShouldBeTrue();
        withCoverage.Value.Dispose();
        withoutCoverage.IsSuccess.ShouldBeTrue();
        withoutCoverage.Value.Dispose();
    }

    private static Result<PipelineHost> Build(
        string job,
        DotNetToolSettings settings,
        Func<string, string?>? environment = null,
        bool dryRun = false)
    {
        var pipeline = new DotNetToolPipeline();
        var builder = PipelineHostBuilderHelpers.Create(pipeline.Name, environment, dryRun);
        pipeline.Configure(builder, settings);
        return builder.Build(job);
    }
}
