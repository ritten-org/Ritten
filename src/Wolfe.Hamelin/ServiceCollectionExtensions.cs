using Microsoft.Extensions.DependencyInjection;
using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="ICommandRunner"/>to the service collection.
        /// </summary>
        public IServiceCollection AddCommandRunner() => services.AddSingleton<ICommandRunner, CommandRunner>();
    }
}
