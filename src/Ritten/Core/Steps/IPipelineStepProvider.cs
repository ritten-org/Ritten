using Ritten.Contracts;

namespace Ritten.Core.Steps;

internal interface IPipelineStepProvider
{
    IEnumerable<IPipelineStep> GetSteps();
}
