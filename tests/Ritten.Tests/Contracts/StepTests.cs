using Ritten.Contracts;

namespace Ritten.Tests.Contracts;

public class StepTests
{
    [Fact]
    public void FromType_ReadsTheStepsFacts()
    {
        var step = Step.FromType<ProducingStep>();

        step.Name.ShouldBe("producer");
        step.Kind.ShouldBe(StepKind.Work);
        step.Produces.ShouldBe(typeof(Produced));
        step.Requires.ShouldBe([typeof(Consumed)]);
    }

    [Fact]
    public void FromType_RejectsAClassWithoutAStepAttribute()
    {
        // Name and kind are required, not defaulted: an unclassified step is a mistake, not work.
        Should.Throw<InvalidOperationException>(() => Step.FromType<Unclassified>())
            .Message.ShouldContain("[Step]");
    }

    [Fact]
    public void FromType_RejectsAStepWithoutARunMethod()
    {
        Should.Throw<InvalidOperationException>(() => Step.FromType<Runless>())
            .Message.ShouldContain("Run");
    }

    [Fact]
    public void FromType_RejectsARunMethodReturningTheWrongType()
    {
        Should.Throw<InvalidOperationException>(() => Step.FromType<WrongReturn>())
            .Message.ShouldContain("StepResult");
    }

    private sealed record Produced;

    private sealed record Consumed;

    [Step("producer", StepKind.Work)]
    private sealed class ProducingStep
    {
        public StepResult<Produced> Run(Consumed consumed) => new Produced();
    }

    private sealed class Unclassified
    {
        public StepResult Run() => StepResult.Successful;
    }

    [Step("runless", StepKind.Work)]
    private sealed class Runless;

    [Step("wrong", StepKind.Work)]
    private sealed class WrongReturn
    {
        public int Run() => 1;
    }
}
