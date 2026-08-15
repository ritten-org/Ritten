using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Rules;

namespace Ritten.Tests.Core.Rules;

public class ProduceBeforeConsumeTests
{
    private sealed record ProducedValue;

    [Fact]
    public void FailsAConsumerWithNoProducerBeforeIt()
    {
        var errors = Rule().Check([
            Step(name: "consumer", requires: typeof(ProducedValue)),
            Step(name: "producer", produces: typeof(ProducedValue))
        ]);

        var error = errors.ShouldHaveSingleItem();
        error.Message.ShouldContain("consumer");
        error.Message.ShouldContain("no earlier step produces");
    }

    [Fact]
    public void PassesAConsumerAfterItsProducer()
    {
        var errors = Rule().Check([
            Step(produces: typeof(ProducedValue)),
            Step(requires: typeof(ProducedValue))
        ]);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void FallsBackToTheContainerForUnproducedParameters()
    {
        var services = new ServiceCollection().AddSingleton(new ProducedValue()).BuildServiceProvider();

        var errors = Rule(services).Check([Step(requires: typeof(ProducedValue))]);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsEveryUnsatisfiedParameterAtOnce()
    {
        var errors = Rule().Check([
            Step(name: "first", requires: typeof(ProducedValue)),
            Step(name: "second", requires: typeof(ProducedValue))
        ]);

        errors.Count().ShouldBe(2);
    }

    private static ProduceBeforeConsume Rule(IServiceProvider? services = null) =>
        new(services ?? new ServiceCollection().BuildServiceProvider());

    private static JobStep Step(string name = "step", Type? produces = null, Type? requires = null) =>
        new(name, StepKind.Work, produces, requires is null ? [] : [requires]);
}
