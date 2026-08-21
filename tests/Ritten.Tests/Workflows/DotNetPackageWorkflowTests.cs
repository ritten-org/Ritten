using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Tests.Engine.Helpers;
using Ritten.Workflows.DotNetPackage;

namespace Ritten.Tests.Workflows;

/// <summary>
/// The package workflow is declared identically to the tool workflow today; these tests pin the
/// wiring (jobs build, requirements hold) so the two can diverge deliberately rather than by rot.
/// </summary>
public class DotNetPackageWorkflowTests
{
    private const string Complete = """{ "build": { "project": "src/Thing/Thing.csproj" } }""";

    [Theory]
    [InlineData("status")]
    [InlineData("build")]
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
    [InlineData("status")]
    [InlineData("prepare")]
    [InlineData("check")]
    [InlineData("deploy")]
    public void ShippingJobs_RequireAProject(string job)
    {
        var result = Build(job, "{}");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("'build.project'");
    }

    private static Result<WorkflowRun> Build(string job, string settings)
    {
        var workflow = new DotNetPackageWorkflow();
        var builder = WorkflowRunBuilderHelpers.Create(workflow.Label, settings: settings);
        return builder.Build(workflow.Jobs.Single(j => j.Name == job));
    }
}
