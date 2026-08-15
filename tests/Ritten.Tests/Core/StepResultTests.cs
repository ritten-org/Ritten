using Ritten.Contracts;
using Ritten.Core;

namespace Ritten.Tests.Core;

public class StepResultTests
{
    public static TheoryData<StepResult> Failures =>
    [
        StepResult.StoppedAfterCancel,
        StepResult.Failed("Something went wrong.")
    ];

    [Theory]
    [MemberData(nameof(Failures))]
    public void EveryFailure_CarriesAtLeastOneError(StepResult result)
    {
        // IsFailure promises callers that Errors is non-null, so anything failing by exit code
        // has to have one — the reporter walks them without checking.
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldNotBeEmpty();
    }

    [Fact]
    public void Successful_HasNoErrors()
    {
        StepResult.Successful.IsFailure.ShouldBeFalse();
        StepResult.Successful.Errors.ShouldBeNull();
    }

    [Fact]
    public void NothingToDo_StopsThePipelineSuccessfully()
    {
        StepResult.NothingToDo.IsFailure.ShouldBeFalse();
        StepResult.NothingToDo.Continue.ShouldBeFalse();
        StepResult.NothingToDo.Errors.ShouldBeNull();
    }

    [Fact]
    public void ProducingResult_CarriesTheValueOnSuccess()
    {
        StepResult<string> result = "the produced value";

        result.Outcome.ShouldBe(StepResult.Successful);
        result.Value.ShouldBe("the produced value");
    }

    [Fact]
    public void ProducingResult_CarriesAFailureWithoutAValue()
    {
        StepResult<string> result = StepResult.Failed("Nope.");

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void ProducingResult_RefusesAContinuingSuccessWithoutAValue()
    {
        // The whole point of the return type: a producing step can't claim success and produce nothing.
        Should.Throw<InvalidOperationException>(() => { StepResult<string> _ = StepResult.Successful; });
    }

    [Fact]
    public void ProducingResult_AllowsASuccessfulEarlyStopWithoutAValue()
    {
        // Nothing after the stop consumes, so nothing needs producing.
        StepResult<string> result = StepResult.NothingToDo;

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void Failed_KeepsEveryErrorItWasGiven()
    {
        var result = StepResult.Failed([new Error("First."), new Error("Second.")]);

        result.Errors.ShouldNotBeNull().Select(e => e.Message).ShouldBe(["First.", "Second."]);
    }
}
