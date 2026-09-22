using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Runs;

public class WorkflowResultTests
{
    private static readonly Step First = Step.FromType<FirstStep>();

    [Fact]
    public void StoppedAt_IsTheStepThatHadNothingLeftToDo()
    {
        var stopped = new StepOutcome(First, StepResult.NothingToDo);

        new WorkflowResult(ExitCode.Success, [new StepOutcome(First, StepResult.Successful), stopped]).StoppedAt.ShouldBe(stopped);
    }

    [Fact]
    public void StoppedAt_IsNothingWhenEveryStepRanOrOneFailed()
    {
        new WorkflowResult(ExitCode.Success, [new StepOutcome(First, StepResult.Successful)]).StoppedAt.ShouldBeNull();
        new WorkflowResult(ExitCode.Failed, [new StepOutcome(First, StepResult.Failed(new Error("no")))]).StoppedAt.ShouldBeNull();
        new WorkflowResult(ExitCode.Cancelled, [new StepOutcome(First, StepResult.StoppedAfterCancel)]).StoppedAt.ShouldBeNull();
    }
}
