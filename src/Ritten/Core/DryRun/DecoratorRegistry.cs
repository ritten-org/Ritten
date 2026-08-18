using Microsoft.Extensions.DependencyInjection;

namespace Ritten.Core.DryRun;

/// <summary>
/// The dry-run pairings declared alongside a set of registrations.
/// </summary>
public class DecoratorRegistry
{
    private readonly List<Decorator> _list = [];

    /// <summary>
    /// Registers a service to get replaced wholesale by another type.
    /// </summary>
    /// <typeparam name="TService">The type to replace.</typeparam>
    /// <typeparam name="TReplacement">The service to replace it with.</typeparam>
    public DecoratorRegistry Replace<TService, TReplacement>() where TService : class where TReplacement : class, TService
    {
        _list.Add(new Decorator(typeof(TService), Replace<TService, TReplacement>));
        return this;
    }

    /// <summary>
    /// Registers a service to get decorated by another.
    /// Decorator types receive the original instance to block irreversible actions and proxy others.
    /// </summary>
    /// <typeparam name="TService">The type to decorate.</typeparam>
    /// <typeparam name="TDecorator">The type to decorate it with.</typeparam>
    public DecoratorRegistry Decorate<TService, TDecorator>() where TService : class where TDecorator : class, TService
    {
        _list.Add(new Decorator(typeof(TService), Decorate<TService, TDecorator>));
        return this;
    }

    /// <summary>
    /// Adopts a decorator declared in another registry.
    /// </summary>
    internal DecoratorRegistry Add(Decorator decorator)
    {
        _list.Add(decorator);
        return this;
    }

    /// <summary>
    /// Gets all the decorators declared by this registry.
    /// </summary>
    internal IReadOnlyCollection<Decorator> GetAll() => _list.AsReadOnly();

    /// <summary>
    /// Replaces a registered service with a decorator that wraps it. Does nothing when the
    /// service isn't registered, since a workflow only registers the capabilities it uses.
    /// </summary>
    private static void Decorate<TService, TDecorator>(IServiceCollection services) where TService : class where TDecorator : class, TService
    {
        if (services.LastOrDefault(d => d.ServiceType == typeof(TService)) is not { } registration)
        {
            return;
        }

        services.Remove(registration);
        services.AddSingleton<TService>(provider =>
        {
            var inner = Resolve<TService>(provider, registration);
            return ActivatorUtilities.CreateInstance<TDecorator>(provider, inner);
        });
    }

    /// <summary>
    /// Replaces a registered service outright, for a stand-in that has no need of the real one.
    /// Does nothing when the service isn't registered.
    /// </summary>
    private static void Replace<TService, TReplacement>(IServiceCollection services) where TService : class where TReplacement : class, TService
    {
        if (services.LastOrDefault(d => d.ServiceType == typeof(TService)) is not { } registration)
        {
            return;
        }

        services.Remove(registration);
        services.AddSingleton<TService, TReplacement>();
    }

    private static TService Resolve<TService>(IServiceProvider provider, ServiceDescriptor registration) where TService : class => registration switch
    {
        { ImplementationInstance: TService instance } => instance,
        { ImplementationFactory: { } factory } => (TService)factory(provider),
        { ImplementationType: { } type } => (TService)ActivatorUtilities.CreateInstance(provider, type),
        _ => throw new InvalidOperationException($"Cannot decorate {typeof(TService).Name}: it has no implementation.")
    };
}
