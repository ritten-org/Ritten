using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core.Runner;

/// <summary>
/// Exposes the interface for running individual pipeline steps.
/// </summary>
internal interface IPipelineStepRunner
{
    /// <summary>
    /// Runs the given step.
    /// </summary>
    Task<StepExecutionSummary> RunStep(AsyncServiceScope scope, IPipelineStep step, CancellationToken cancellationToken = default);
}
