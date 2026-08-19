using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Engine.Runtimes;

/// <summary>
/// Declares an environment a workflow can find itself running in.
/// </summary>
public abstract class Runtime
{
    /// <summary>
    /// The identifier the runtime is registered and reported under.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The environment variables whose presence identifies this runtime.
    /// </summary>
    public abstract IReadOnlyCollection<string> Markers { get; }

    /// <summary>
    /// The environment variables this runtime owns, including its markers.
    /// </summary>
    public abstract IReadOnlyCollection<string> Claims { get; }

    /// <summary>
    /// Registers the services and anything else this runtime provides.
    /// </summary>
    /// <param name="builder">The configuration area: services and dry-run decorators.</param>
    /// <param name="environment">The unfiltered environment; the runtime owns its claims.</param>
    public abstract void Configure(IWorkflowBuilder builder, Func<string, string?> environment);

    /// <summary>
    /// Whether this environment has asked for debug logging.
    /// </summary>
    /// <param name="environment">The unfiltered environment; the runtime owns its claims.</param>
    public virtual bool IsDebug(Func<string, string?> environment) => false;

    /// <summary>
    /// Creates the console narrative for a run in this environment.
    /// </summary>
    /// <param name="level">The lowest level of message the console should print.</param>
    public virtual IWorkflowConsole CreateConsole(WorkflowLogLevel level) =>
        new SpectreWorkflowConsole(AnsiConsole.Console, level);
}
