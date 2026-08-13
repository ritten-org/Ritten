using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core.Steps;

internal class PipelineStepProvider(IServiceProvider serviceProvider, PipelineStepTypes stepTypes) : IPipelineStepProvider
{
    public IEnumerable<IPipelineStep> GetSteps() =>
        stepTypes.Steps.Select(t => (IPipelineStep)serviceProvider.GetRequiredService(t));
}
