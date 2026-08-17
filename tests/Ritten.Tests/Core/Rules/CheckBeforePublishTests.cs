using Ritten.Contracts;
using Ritten.Core.Rules;
using Ritten.Tests.Support;

namespace Ritten.Tests.Core.Rules;

public class CheckBeforePublishTests
{
    private readonly CheckBeforePublish _rule = new();

    [Fact]
    public void FailsACheckThatRunsAfterAPublish()
    {
        var errors = _rule.Check(new TestJob(steps: [
            Step(StepKind.Publish),
            Step(StepKind.Check, "changelog")
        ]));

        errors.ShouldHaveSingleItem().Message.ShouldContain("changelog");
    }

    [Fact]
    public void ReportsEveryLateCheck()
    {
        var errors = _rule.Check(new TestJob(steps: [
            Step(StepKind.Publish),
            Step(StepKind.Check, "first"),
            Step(StepKind.Check, "second")
        ]));

        errors.Count().ShouldBe(2);
    }

    [Fact]
    public void PassesChecksAheadOfThePublishSteps()
    {
        var errors = _rule.Check(new TestJob(steps: [
            Step(StepKind.Check),
            Step(StepKind.Gate),
            Step(StepKind.Publish)
        ]));

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void PassesAJobThatPublishesNothing()
    {
        var errors = _rule.Check(new TestJob(steps: [Step(StepKind.Work), Step(StepKind.Check)]));

        errors.ShouldBeEmpty();
    }

    private static Step Step(StepKind kind, string name = "step") => new(name, kind, null, []);
}
