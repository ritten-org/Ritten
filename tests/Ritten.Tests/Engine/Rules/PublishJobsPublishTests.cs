using Ritten.Contracts;
using Ritten.Engine.Rules;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Rules;

public class PublishJobsPublishTests
{
    [Fact]
    public void AllowsAPublishingJobThatPublishes()
    {
        var job = Job(JobKind.Publish, Step("nuget push", StepKind.Publish));

        new PublishJobsPublish().Check(job).ShouldBeEmpty();
    }

    [Fact]
    public void RefusesAPublishingJobThatPublishesNothing()
    {
        // A deploy that forgot its push would report success having released nothing.
        var job = Job(JobKind.Publish, Step("dotnet pack", StepKind.Work));

        new PublishJobsPublish().Check(job).ShouldHaveSingleItem()
            .Message.ShouldBe("The deploy job publishes, but none of its steps do. Give it a publish step, or declare it another kind.");
    }

    [Fact]
    public void RefusesACheckingJobThatPublishes()
    {
        // The dangerous one: a check runs on every change, so this would release from every pull request.
        var job = Job(JobKind.Check, Step("dotnet test", StepKind.Work), Step("nuget push", StepKind.Publish));

        new PublishJobsPublish().Check(job).ShouldHaveSingleItem()
            .Message.ShouldBe("The deploy job only checks, but 'nuget push' publishes. A job that runs on every change must not release.");
    }

    [Fact]
    public void LeavesWorkJobsAlone()
    {
        // Install and prepare do real work without releasing, and neither claim says anything.
        new PublishJobsPublish().Check(Job(JobKind.Work, Step("dotnet build", StepKind.Work))).ShouldBeEmpty();
    }

    private static Step Step(string name, StepKind kind) => new(name, kind, null, []);

    private static TestJob Job(JobKind kind, params Step[] steps) => new("deploy", steps, kind: kind);
}
