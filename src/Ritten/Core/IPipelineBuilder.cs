using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// Provides methods for configuring services and declaring steps for a pipeline.
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// The service collection for registering dependencies.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Appends a step to the pipeline. Steps run in the order they are declared.
    /// </summary>
    /// <typeparam name="TStep">The step type. It is automatically registered in the service collection.</typeparam>
    IPipelineBuilder UseStep<TStep>() where TStep : class, IPipelineStep;
}
