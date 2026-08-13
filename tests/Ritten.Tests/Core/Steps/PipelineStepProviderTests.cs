using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Steps;

namespace Ritten.Tests.Core.Steps;

public class PipelineStepProviderTests
{
    [Fact]
    public void GetSteps_WithSteps_ReturnsCorrectSteps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DummyStep1>();
        services.AddTransient<DummyStep2>();
        var serviceProvider = services.BuildServiceProvider();

        var stepTypes = new PipelineStepTypes([typeof(DummyStep1), typeof(DummyStep2)]);

        var provider = new PipelineStepProvider(serviceProvider, stepTypes);

        // Act
        var steps = provider.GetSteps().ToList();

        // Assert
        steps.ShouldBeOfTypes(typeof(DummyStep1), typeof(DummyStep2));
    }

    private class DummyStep1 : IPipelineStep
    {
        public Task<StepResult> Run(CancellationToken cancellationToken = default) => Task.FromResult(StepResult.Successful);
    }

    private class DummyStep2 : IPipelineStep
    {
        public Task<StepResult> Run(CancellationToken cancellationToken = default) => Task.FromResult(StepResult.Successful);
    }
}
