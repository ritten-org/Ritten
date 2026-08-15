using Ritten.Contracts;
using Ritten.Core.Rules;

namespace Ritten.Tests.Core.Rules;

public class GateBeforePublishTests
{
    private readonly GateBeforePublish _rule = new();

    [Fact]
    public void FailsAPublishWithNoGateBeforeIt()
    {
        var errors = _rule.Check([Step(StepKind.Work), Step(StepKind.Publish, "git tag")]);

        errors.ShouldHaveSingleItem().Message.ShouldContain("git tag");
    }

    [Fact]
    public void AGateAfterThePublishDoesNotCount()
    {
        // The gate has to be able to stop the publish, not regret it.
        var errors = _rule.Check([Step(StepKind.Publish, "git tag"), Step(StepKind.Gate)]);

        errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void PassesWhenAGateRunsBeforeTheFirstPublish()
    {
        var errors = _rule.Check([
            Step(StepKind.Gate),
            Step(StepKind.Publish),
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
