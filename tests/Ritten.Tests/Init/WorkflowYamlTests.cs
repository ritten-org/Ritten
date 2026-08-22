using Ritten.Contracts;
using Ritten.Init;
using Ritten.Tests.Support;

namespace Ritten.Tests.Init;

/// <summary>
/// What runs when, and with what permissions, is read from the job model — so these assert the
/// derivation rather than the exact YAML, which is free to be reworded.
/// </summary>
public class WorkflowYamlTests
{
    [Fact]
    public void ACheckingJobRunsOnEveryChange()
    {
        var yaml = WorkflowYaml.Render(new TestWorkflow(jobs: [Job("check", JobKind.Check)]));

        yaml.ShouldContain("pull_request:");
        yaml.ShouldContain("push:");
        yaml.ShouldContain("run: dotnet ritten check");

        // It posts the report as a comment, so it needs to be able to write one.
        yaml.ShouldContain("pull-requests: write");

        // A newer push supersedes a check still running.
        yaml.ShouldContain("cancel-in-progress: true");
    }

    [Fact]
    public void ADeployingJobRunsOnlyWhenAsked()
    {
        var yaml = WorkflowYaml.Render(new TestWorkflow(jobs: [Job("deploy", JobKind.Deploy)]));

        yaml.ShouldContain("workflow_dispatch:");
        yaml.ShouldContain("github.event_name == 'workflow_dispatch'");

        // Tags and releases are written back, and releases queue rather than interleave.
        yaml.ShouldContain("contents: write");
        yaml.ShouldContain("cancel-in-progress: false");

        // Nobody is at the terminal to confirm a release.
        yaml.ShouldContain("dotnet ritten deploy --auto-approve");
    }

    [Fact]
    public void AWorkflowThatReleasesNothingGetsNoDeployJob()
    {
        // The whole point of deriving it: a repository is never offered CI it can't run.
        var yaml = WorkflowYaml.Render(new TestWorkflow(jobs: [Job("build", JobKind.Work), Job("check", JobKind.Check)]));

        yaml.ShouldNotContain("workflow_dispatch");
        yaml.ShouldNotContain("contents: write");
        yaml.ShouldNotContain("NUGET");
    }

    [Fact]
    public void WorkJobsAreLeftToThePersonWhoWantsThem()
    {
        // status, build, install and prepare are run by hand; scaffolding them would be noise.
        var yaml = WorkflowYaml.Render(new TestWorkflow(jobs: [Job("install", JobKind.Work), Job("prepare", JobKind.Work)]));

        yaml.ShouldNotContain("install");
        yaml.ShouldNotContain("prepare");
    }

    private static TestJob Job(string name, JobKind kind) => new(name, kind: kind);
}
