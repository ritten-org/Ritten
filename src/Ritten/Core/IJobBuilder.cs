using System.Runtime.CompilerServices;
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
    /// <param name="value">The setting, read straight off the settings object: <c>Requires(settings.Build.Project)</c>.</param>
    /// <param name="expression">Supplied by the compiler; don't pass this.</param>
    IJobBuilder Requires(string? value, [CallerArgumentExpression(nameof(value))] string expression = "");

    /// <summary>
    /// Appends a step to the job. Steps run in the order they are declared.
    /// </summary>
    /// <typeparam name="TStep">The step type. It is automatically registered in the service collection.</typeparam>
    IJobBuilder UseStep<TStep>() where TStep : class, IPipelineStep;
}
