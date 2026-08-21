using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Tests.Engine.Helpers;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Workflows;

/// <summary>
/// The .NET tool workflow's jobs read the same project settings but don't share requirements:
/// only the jobs that ship a package need to know which project to pack.
/// </summary>
public class DotNetToolWorkflowTests
{
    private const string Complete = """{ "build": { "project": "src/Thing/Thing.csproj" } }""";

    [Fact]
    public void DeclaresTheJobsTheCliOffers()
    {
        new DotNetToolWorkflow().Jobs.Select(j => j.Name).ShouldBe(["status", "build", "install", "prepare", "check", "deploy"]);
    }

    [Theory]
    [InlineData("status")]
    [InlineData("build")]
    [InlineData("install")]
    [InlineData("prepare")]
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
        var result = Build("build", "{}");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Theory]
    [InlineData("status")]
    [InlineData("install")]
    [InlineData("prepare")]
    [InlineData("check")]
    [InlineData("deploy")]
    public void ShippingJobs_RequireAProject(string job)
    {
        var result = Build(job, "{}");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("'build.project'");
    }

    [Theory]
    [InlineData("status")]
    [InlineData("install")]
    [InlineData("prepare")]
    [InlineData("check")]
    [InlineData("deploy")]
    public void ShippingJobs_AcceptProjectsAsThePluralSpelling(string job)
    {
        var result = Build(job, """{ "build": { "projects": ["src/Core/Core.csproj", "src/Thing/Thing.csproj"] } }""");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    [Fact]
    public void BothProjectSpellingsAreRefused()
    {
        // `project` and `projects` are two spellings of one setting; declaring both is ambiguous.
        var result = Build("deploy", """{ "build": { "project": "src/A/A.csproj", "projects": ["src/B/B.csproj"] } }""");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("use one");
    }

    [Theory]
    [InlineData("prepare")]
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
        var result = Build("deploy", Complete, environment: WorkflowRunBuilderHelpers.Empty);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dispose();
    }

    private static Result<WorkflowRun> Build(
        string job,
        string settings,
        Func<string, string?>? environment = null,
        bool dryRun = false)
    {
        var workflow = new DotNetToolWorkflow();
        var builder = WorkflowRunBuilderHelpers.Create(workflow.Label, environment, dryRun, settings: settings);
        return builder.Build(workflow.Jobs.Single(j => j.Name == job));
    }
}
