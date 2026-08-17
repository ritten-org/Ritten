using Microsoft.Extensions.DependencyInjection;

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
}
