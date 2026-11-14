using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;

namespace Ritten.NSchema;

/// <summary>
/// Registers the NSchema client in any host.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the NSchema client, run as <paramref name="configure"/> says: an application that
        /// migrates its own database at start uses this.
        /// </summary>
        public IServiceCollection AddNSchema(Action<NSchemaOptions>? configure = null)
        {
            services.AddCommandRunner();
            var options = services.AddOptions<NSchemaOptions>();
            if (configure is not null)
            {
                options.Configure(configure);
            }

            services.TryAddScoped<INSchema, NSchemaClient>();
            return services;
        }
    }
}
