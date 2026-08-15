using Microsoft.Extensions.DependencyInjection;
using Ritten.Pipelines;

namespace Ritten.CodeCoverage;

/// <summary>
/// Registers the coverage domain.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds coverage collection and thresholds, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddCoverage(CoverageSettings settings)
        {
            services.Configure<CoverageOptions>(o =>
            {
                o.MinimumLine = settings.Line;
                o.MinimumBranch = settings.Branch;
            });
            return services;
        }
    }
}
