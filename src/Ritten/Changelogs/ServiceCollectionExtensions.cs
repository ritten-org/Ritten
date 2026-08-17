using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Pipelines;

namespace Ritten.Changelogs;

/// <summary>
/// Registers the changelog domain.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds changelog checks.
        /// </summary>
        public IServiceCollection AddChangelogs(ChangelogSettings settings)
        {
            services.TryAddSingleton<IChangelog, ChangelogClient>();
            services.Configure<ChangelogOptions>(o => o.File = settings.File);
            return services;
        }
    }
}
