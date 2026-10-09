using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Engine.DryRun;

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
        _list.Add(new Decorator(typeof(TService), (provider, _) => ActivatorUtilities.CreateInstance<TReplacement>(provider)));
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
        _list.Add(new Decorator(typeof(TService), (provider, inner) => ActivatorUtilities.CreateInstance<TDecorator>(provider, inner)));
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
    /// Makes each paired client choose its form as it is resolved: the rehearsal one in a dry run, the real one
    /// otherwise. The last pairing declared for a client wins; one for a client nobody registered does nothing.
    /// </summary>
    /// <param name="services">Every registration the application makes.</param>
    internal void Apply(IServiceCollection services)
    {
        foreach (var decorator in _list.GroupBy(d => d.ServiceType).Select(g => g.Last()))
        {
            if (services.LastOrDefault(d => d.ServiceType == decorator.ServiceType && !d.IsKeyedService) is not { } registration)
            {
                continue;
            }

            // The real client moves under a key of its own, keeping its lifetime, and the pairing takes its place.
            var key = new object();
            services.Remove(registration);
            services.Add(Keyed(registration, key));
            services.AddScoped(decorator.ServiceType, provider =>
            {
                var inner = provider.GetRequiredKeyedService(decorator.ServiceType, key);
                return provider.GetRequiredService<WorkflowJob>().DryRun ? decorator.Create(provider, inner) : inner;
            });
        }
    }

    private static ServiceDescriptor Keyed(ServiceDescriptor registration, object key) => registration switch
    {
        { ImplementationInstance: { } instance } => new ServiceDescriptor(registration.ServiceType, key, instance),
        { ImplementationFactory: { } factory } => new ServiceDescriptor(registration.ServiceType, key, (provider, _) => factory(provider), registration.Lifetime),
        { ImplementationType: { } type } => new ServiceDescriptor(registration.ServiceType, key, type, registration.Lifetime),
        _ => throw new InvalidOperationException($"Cannot decorate {registration.ServiceType.Name}: it has no implementation.")
    };
}
