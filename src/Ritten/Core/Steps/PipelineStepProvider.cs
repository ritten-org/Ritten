using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core.Steps;

internal class PipelineStepProvider(IServiceProvider serviceProvider, IPipelineStepCollection steps) : IPipelineStepProvider
{
    public IEnumerable<IPipelineStep> GetSteps()
    {
        return steps.Steps.Select(t => (IPipelineStep)serviceProvider.GetRequiredService(t));
    }
}
