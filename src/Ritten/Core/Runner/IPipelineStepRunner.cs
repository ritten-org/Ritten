using Ritten.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Ritten.Core.Runner;

internal interface IPipelineStepRunner
{
    Task<StepExecutionSummary> RunStep(AsyncServiceScope scope, IPipelineStep step, CancellationToken cancellationToken = default);
}
