using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

/// <summary>
/// The workflow file belongs to the repository, so what these tests pin is what survives being
/// written back: other jobs, other triggers, comments, and the shape of everything untouched.
/// </summary>
public class ActionsWorkflowTests
{
    private const string Existing =
        """
        # Ours, hand tended.
        name: CI

        on:
          push:
            branches: [ main ]   # only main

        jobs:
          # The important one.
          lint:
            runs-on: ubuntu-latest
            steps:
              - run: make lint

          check:
            runs-on: ubuntu-latest
            steps:
              - name: Run check
                run: dotnet ritten check
                working-directory: services/api

        """;

    private const string Check =
        """
          check:
            runs-on: ubuntu-24.04
            steps:
              - run: dotnet ritten check
        """;

    [Fact]
    public void ReadsWhatTheWorkflowRunsAndWhere()
    {
        var workflow = ActionsWorkflow.Parse(Existing);

        workflow.Name.ShouldBe("CI");
        workflow.Triggers.ShouldBe(["push"]);
        workflow.Jobs.Select(job => job.Id).ShouldBe(["lint", "check"]);

        // What a job runs is how the tool that wrote it recognises its own, and the working
        // directory is what tells one project's job from another's.
        var check = workflow.Jobs.Last();
        check.Invokes("dotnet ritten check").ShouldBeTrue();
        check.Steps.ShouldHaveSingleItem().WorkingDirectory.ShouldBe("services/api");
    }

    [Fact]
    public void ReplacesTheJobItOwnsAndNothingElse()
    {
        var written = ActionsWorkflow.Parse(Existing).WithJob("check", Check).Text;

        written.ShouldContain("runs-on: ubuntu-24.04");
        written.ShouldNotContain("working-directory: services/api");

        // Everything that isn't the job stays exactly as the repository wrote it.
        written.ShouldContain("# Ours, hand tended.");
        written.ShouldContain("branches: [ main ]   # only main");
        written.ShouldContain("# The important one.");
        written.ShouldContain("- run: make lint");
    }

    [Fact]
    public void AddsAJobTheWorkflowDoesNotHave()
    {
        var written = ActionsWorkflow.Parse(Existing).WithJob("deploy", "  deploy:\n    runs-on: ubuntu-latest").Text;

        ActionsWorkflow.Parse(written).Jobs.Select(job => job.Id).ShouldBe(["lint", "check", "deploy"]);
        written.ShouldContain("- run: make lint");
    }

    [Fact]
    public void AddsATriggerWithoutTouchingTheOnesAlreadyThere()
    {
        var written = ActionsWorkflow.Parse(Existing)
            .WithTrigger("push", "  push:\n    branches: [ trunk ]")
            .WithTrigger("pull_request", "  pull_request:\n    branches: [ main ]")
            .Text;

        // A repository that narrowed its own branches meant to.
        written.ShouldContain("branches: [ main ]   # only main");
        written.ShouldNotContain("trunk");
        ActionsWorkflow.Parse(written).Triggers.ShouldBe(["push", "pull_request"]);
    }

    [Fact]
    public void WritingTheSameJobTwiceChangesNothing()
    {
        var once = ActionsWorkflow.Parse(Existing).WithJob("check", Check);

        once.WithJob("check", Check).Text.ShouldBe(once.Text);
    }

    [Fact]
    public void BuildsAWorkflowFromNothing()
    {
        var written = ActionsWorkflow
            .Parse("name: Ritten\n\non:\n\njobs:\n")
            .WithTrigger("push", "  push:\n    branches: [ main ]")
            .WithJob("check", Check)
            .Text;

        var parsed = ActionsWorkflow.Parse(written);
        parsed.Name.ShouldBe("Ritten");
        parsed.Triggers.ShouldBe(["push"]);
        parsed.Jobs.ShouldHaveSingleItem().Id.ShouldBe("check");
    }

    [Fact]
    public void ReadsTheWorkingDirectoryEveryStepDefaultsTo()
    {
        var workflow = ActionsWorkflow.Parse(
            """
            name: CI

            defaults:
              run:
                working-directory: services/web

            jobs:
              check:
                steps:
                  - run: dotnet ritten check
            """);

        workflow.WorkingDirectory.ShouldBe("services/web");
    }
}
