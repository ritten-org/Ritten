using Ritten.Contracts;
using Ritten.Core.Rules;

namespace Ritten.Tests.Core.Rules;

public class ValidationBeforePublishTests
{
    private readonly ValidationBeforePublish _rule = new();

    [Fact]
    public void FailsAValidationThatRunsAfterAPublish()
    {
        var errors = _rule.Check([
            Step(StepKind.Publish),
            Step(StepKind.Validation, "changelog")
        ]);

        errors.ShouldHaveSingleItem().Message.ShouldContain("changelog");
    }

    [Fact]
    public void ReportsEveryLateValidation()
    {
        var errors = _rule.Check([
            Step(StepKind.Publish),
            Step(StepKind.Validation, "first"),
            Step(StepKind.Validation, "second")
        ]);

        errors.Count().ShouldBe(2);
    }

    [Fact]
    public void PassesValidationsAheadOfThePublishSteps()
    {
        var errors = _rule.Check([
            Step(StepKind.Validation),
            Step(StepKind.Gate),
            Step(StepKind.Publish)
        ]);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void PassesAJobThatPublishesNothing()
    {
        var errors = _rule.Check([Step(StepKind.Work), Step(StepKind.Validation)]);

        errors.ShouldBeEmpty();
    }

    private static JobStep Step(StepKind kind, string name = "step") => new(name, kind, null, []);
}
