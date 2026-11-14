using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;

namespace Ritten.Npm;

/// <summary>
/// Registers the npm client in any host.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the npm client.
        /// </summary>
        public IServiceCollection AddNpm()
        {
            services.AddCommandRunner();
            services.TryAddScoped<INpm, Npm>();
            return services;
        }
    }
}
