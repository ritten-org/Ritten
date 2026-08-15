using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;

namespace Ritten.Git;

/// <summary>
/// Registers the git domain.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds release tagging, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddGit(string tagPrefix)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IGit, GitClient>();
            services.Configure<GitOptions>(o => o.TagPrefix = tagPrefix);
            services.Configure<GitOptions>(GitOptions.ConfigureFromEnvironment);
            return services;
        }
    }
}
