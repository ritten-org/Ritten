using Microsoft.Extensions.DependencyInjection;

namespace Ritten.Core.DryRun;

/// <summary>
/// Pairs an outward-reaching client with its offline/dry run version.
/// </summary>
public sealed class Decorator
{
    internal Decorator(Type serviceType, Action<IServiceCollection> decorate)
    {
        ServiceType = serviceType;
        Decorate = decorate;
    }

    /// <summary>
    /// The client interface the pairing neuters.
    /// </summary>
    internal Type ServiceType { get; }

    /// <summary>
    /// Rewrites the client's registration into its rehearsal form.
    /// </summary>
    internal Action<IServiceCollection> Decorate { get; }
}
