using Microsoft.Extensions.DependencyInjection;

namespace Ritten.Core;

/// <summary>
/// Provides methods for configuring services and declaring the jobs of a pipeline.
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// The service collection for registering dependencies.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Declares a job.
    /// </summary>
    /// <param name="name">The job's name, as given on the command line.</param>
    /// <param name="configure">Declares the job's requirements and steps.</param>
    /// <remarks>A run executes exactly one job, so only the requested job's steps are registered.</remarks>
    IPipelineBuilder AddJob(string name, Action<IJobBuilder> configure);
}
