using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// Declares the requirements and steps of a single job.
/// </summary>
public interface IJobBuilder
{
    /// <summary>
    /// Requires a setting to be present for the job to run.
    /// </summary>
    /// <param name="value">The value read from the project's settings.</param>
    /// <param name="key">The key in <c>ritten.json</c>, for the error message, e.g. <c>build.project</c>.</param>
    IJobBuilder Requires(string? value, string key);

    /// <summary>
    /// Appends a step to the job. Steps run in the order they are declared.
    /// </summary>
    /// <typeparam name="TStep">The step type. It is automatically registered in the service collection.</typeparam>
    IJobBuilder UseStep<TStep>() where TStep : class, IPipelineStep;
}
