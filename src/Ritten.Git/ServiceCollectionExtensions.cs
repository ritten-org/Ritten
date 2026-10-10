using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;

namespace Ritten.Git;

/// <summary>
/// Registers the git client with any host's services, inside a workflow application or not.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the git client, which acts on whichever repository each call names.
        /// </summary>
        public IServiceCollection AddGit()
        {
            services.AddCommandRunner();
            services.TryAddScoped<IGit, GitClient>();
            return services;
        }
    }
}
