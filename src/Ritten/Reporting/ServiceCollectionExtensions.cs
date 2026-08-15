using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.GitHub;

namespace Ritten.Reporting;

/// <summary>
/// Registers build reporting.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="IBuildReport"/> to the service collection and registers the
        /// <see cref="BuildReportPublisher"/> that publishes it when the pipeline finishes.
        /// </summary>
        public IServiceCollection AddBuildReporting()
        {
            services.AddGitHubActionsRuntime();
            if (services.Any(d => d.ServiceType == typeof(BuildReportingMarker)))
            {
                return services;
            }

            services.AddSingleton<BuildReportingMarker>();
            services.AddSingleton<IBuildReport, BuildReport>();
            services.AddSingleton<MarkdownReportRenderer>();
            services.AddSingleton<IProgressReporter, BuildReportPublisher>();
            return services;
        }
    }

    // Enumerable registrations are additive, so this one can't use TryAdd. It keys idempotence
    // off a private marker rather than off a service it happens to register, so that a consumer
    // supplying their own implementation can't silently suppress the rest of the block.
    private sealed class BuildReportingMarker;
}
