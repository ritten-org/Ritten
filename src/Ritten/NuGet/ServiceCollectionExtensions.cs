using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Releases;

namespace Ritten.NuGet;

/// <summary>
/// Registers the NuGet domain.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds NuGet publishing, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddNuGet(string feed, ReleaseLine lines)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<INuGet, NuGetClient>();
            services.Configure<NuGetOptions>(o =>
            {
                o.Feed = feed;
                o.Lines = lines;
            });
            services.Configure<NuGetOptions>(NuGetOptions.ConfigureFromEnvironment);
            return services;
        }
    }
}
