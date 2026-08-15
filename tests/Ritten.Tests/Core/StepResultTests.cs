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
    public void Failed_KeepsEveryErrorItWasGiven()
    {
        var result = StepResult.Failed([new Error("First."), new Error("Second.")]);

        result.Errors.ShouldNotBeNull().Select(e => e.Message).ShouldBe(["First.", "Second."]);
    }
}
