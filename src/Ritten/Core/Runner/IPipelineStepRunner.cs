using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core.Runner;

internal interface IPipelineStepRunner
{
    Task<StepExecutionSummary> RunStep(AsyncServiceScope scope, IPipelineStep step, CancellationToken cancellationToken = default);
}
