using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Engine.Workflows;

/// <summary>
/// The base for declaring a job that runs with arguments.
/// </summary>
/// <remarks>
/// A job is a declaration: it names its steps and the arguments they need, and never registers services.
/// Each property of <typeparamref name="TArguments"/> is put into the run's state under its type before the first
/// step, so steps receive them as <c>Run</c> parameters, exactly like a value an earlier step produced.
/// </remarks>
/// <typeparam name="TArguments">What the job needs to run: read from the project file and command line by the Ritten
/// CLI, or constructed by whoever runs the job directly.</typeparam>
public abstract class Job<TArguments> : IJob where TArguments : class
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract JobKind Kind { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<Step> Steps { get; }

    /// <inheritdoc />
    public Type ArgumentsType => typeof(TArguments);

    /// <inheritdoc />
    public virtual bool RequiresProject => true;

    /// <inheritdoc />
    public virtual bool Reports => true;

    /// <summary>
    /// Judges the arguments the run arrived with, e.g. <c>arguments.Require(a =&gt; a.Build.Project)</c>.
    /// Runs before anything is assembled.
    /// </summary>
    /// <param name="arguments">The validator, holding the arguments and the environment.</param>
    protected virtual void Validate(ArgumentsValidator<TArguments> arguments)
    {
    }

    IReadOnlyList<Error> IJob.Validate(object arguments, Func<string, string?> environment, bool dryRun, IWorkflowLog log, string source)
    {
        var validator = new ArgumentsValidator<TArguments>((TArguments)arguments, environment, dryRun, log, source);
        Validate(validator);
        return validator.Errors;
    }
}

/// <summary>
/// The base for declaring a job that runs with no arguments.
/// </summary>
public abstract class Job : Job<NoArguments>;
