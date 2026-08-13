using Ritten.Contracts;
using Ritten.Core.Steps;

namespace Ritten.Tests.Core.Steps;

public class PipelineStepCollectionTests
{
    [Fact]
    public void Steps_WithSteps_ReturnsCorrectSteps()
    {
        // Arrange
        var collection = new PipelineStepCollection();
        collection.AddStep(typeof(DummyStep1));
        collection.AddStep(typeof(DummyStep2));

        // Act
        var steps = collection.Steps;

        // Assert
        steps.ShouldBe(new[] { typeof(DummyStep1), typeof(DummyStep2) });
    }

    private class DummyStep1 : IPipelineStep
    {
        public Task Run(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class DummyStep2 : IPipelineStep
    {
        public Task Run(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
