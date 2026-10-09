namespace Ritten.Engine.DryRun;

/// <summary>
/// Pairs an outward-reaching client with its offline/dry run version.
/// </summary>
public sealed class Decorator
{
    internal Decorator(Type serviceType, Func<IServiceProvider, object, object> create)
    {
        ServiceType = serviceType;
        Create = create;
    }

    /// <summary>
    /// The client interface the pairing neuters.
    /// </summary>
    internal Type ServiceType { get; }

    /// <summary>
    /// Creates the rehearsal form of the client, given the real one.
    /// </summary>
    internal Func<IServiceProvider, object, object> Create { get; }
}
