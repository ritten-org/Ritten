using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ritten.Commands;

/// <summary>
/// Registers the command runner.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="ICommandRunner"/> to the service collection.
        /// </summary>
        public IServiceCollection AddCommandRunner()
        {
            services.TryAddSingleton<ICommandRunner, CommandRunner>();
            return services;
        }
    }
}
