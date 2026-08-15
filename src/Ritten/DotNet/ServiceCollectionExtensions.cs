using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Pipelines;

namespace Ritten.DotNet;

/// <summary>
/// Registers the .NET domain.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the .NET client and build settings, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddDotNet(DotNetBuildSettings settings)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IDotNet, DotNetClient>();
            services.Configure<DotNetOptions>(o =>
            {
                o.Configuration = settings.Configuration;
                o.ProjectFile = settings.Project ?? "";
            });
            return services;
        }

        /// <summary>
        /// Adds coverage collection and thresholds, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddCoverage(CoverageSettings? settings)
        {
            services.Configure<CoverageOptions>(o =>
            {
                o.Enabled = settings is not null;
                o.MinimumLine = settings?.Line;
                o.MinimumBranch = settings?.Branch;
            });
            return services;
        }
    }
}
