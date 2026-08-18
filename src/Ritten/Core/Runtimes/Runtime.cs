using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Core.Runtimes;

/// <summary>
/// Declares an environment a pipeline can find itself running in.
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
    /// Registers the services this runtime provides.
    /// </summary>
    /// <param name="services">The service collection the job is assembled into.</param>
    /// <param name="environment">The unfiltered environment; the runtime owns its claims.</param>
    public abstract void ConfigureServices(IServiceCollection services, Func<string, string?> environment);

    /// <summary>
    /// Whether this environment has asked for debug logging. A debug marker is claimed like any
    /// other owned variable, so only the runtime it belongs to may honour it.
    /// </summary>
    /// <param name="environment">The unfiltered environment; the runtime owns its claims.</param>
    public virtual bool IsDebug(Func<string, string?> environment) => false;

    /// <summary>
    /// Creates the console narrative for a run in this environment. The engine's terminal
    /// renderer by default; a runtime with its own log grammar (folding, annotations) overrides.
    /// A factory rather than a registration, because the console must exist before the container
    /// does: host errors and settings validation already speak through it.
    /// </summary>
    /// <param name="level">The lowest level of message the console should print.</param>
    public virtual IPipelineConsole CreateConsole(PipelineLogLevel level) =>
        new SpectreProgressReporter(AnsiConsole.Console, level);
}
