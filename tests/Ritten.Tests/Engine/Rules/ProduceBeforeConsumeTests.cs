using Ritten.Contracts;
using Ritten.Engine.Rules;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Rules;

public class ProduceBeforeConsumeTests
{
    private sealed record ProducedValue;

    [Fact]
    public void FailsAConsumerWithNoProducerBeforeIt()
    {
        var errors = Rule().Check(new TestJob(steps: [
            Step(name: "consumer", requires: typeof(ProducedValue)),
            Step(name: "producer", produces: typeof(ProducedValue))
        ]));

        var error = errors.ShouldHaveSingleItem();
        error.Message.ShouldContain("consumer");
        error.Message.ShouldContain("no earlier step produces");
    }

    [Fact]
    public void PassesAConsumerAfterItsProducer()
    {
        var errors = Rule().Check(new TestJob(steps: [
            Step(produces: typeof(ProducedValue)),
            Step(requires: typeof(ProducedValue))
        ]));

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsEveryUnsatisfiedParameterAtOnce()
    {
        var errors = Rule().Check(new TestJob(steps: [
            Step(name: "first", requires: typeof(ProducedValue)),
            Step(name: "second", requires: typeof(ProducedValue))
        ]));

        errors.Count().ShouldBe(2);
    }

    private static ProduceBeforeConsume Rule() => new();

    private static Step Step(string name = "step", Type? produces = null, Type? requires = null) =>
        new(name, StepKind.Work, produces, requires is null ? [] : [requires]);
}
