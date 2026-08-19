using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;

namespace Ritten.Reporting;

/// <summary>
/// Registers build reporting.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="IWorkflowReport"/> to the service collection and registers the
        /// <see cref="BuildReportPublisher"/> that publishes it when the workflow finishes.
        /// </summary>
        public IWorkflowBuilder AddBuildReporting()
        {
            if (builder.Services.Any(d => d.ServiceType == typeof(BuildReportingMarker)))
            {
                return builder;
            }

            builder.Services.AddSingleton<BuildReportingMarker>();
            builder.Services.AddSingleton<IWorkflowReport, WorkflowReport>();
            builder.Services.AddSingleton<MarkdownReportRenderer>();
            builder.Services.AddSingleton<IProgressReporter, BuildReportPublisher>();
            return builder;
        }
    }

    // Enumerable registrations are additive, so this one can't use TryAdd. It keys idempotence
    // off a private marker rather than off a service it happens to register, so that a consumer
    // supplying their own implementation can't silently suppress the rest of the block.
    private sealed class BuildReportingMarker;
}
