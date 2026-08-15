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
    /// Requires an environment variable to be set, unless this is a dry run.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    IJobBuilder RequiresEnvironment(string variable);

    /// <summary>
    /// Appends a step to the job. Steps run in the order they are declared.
    /// </summary>
    /// <typeparam name="TStep">The step type. It is automatically registered in the service collection.</typeparam>
    /// <remarks>
    /// A step is any class with a <see cref="StepAttribute"/> and a single public <c>Run</c> method whose signature
    /// matches <c>Task&lt;StepResult&lt;T&gt;&gt; Run(..., CancellationToken cancellationToken = default)</c>.
    /// Both are validated when the job is built.
    /// </remarks>
    IJobBuilder UseStep<TStep>() where TStep : class;
}
