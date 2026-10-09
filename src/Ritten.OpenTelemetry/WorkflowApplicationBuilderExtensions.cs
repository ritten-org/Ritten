using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.OpenTelemetry;

/// <summary>
/// Extension methods for <see cref="WorkflowApplicationBuilder"/>.
/// </summary>
public static class WorkflowApplicationBuilderExtensions
{
    /// <summary>
    /// The source HttpClient emits its own spans from, so a step's calls to GitHub, NuGet or Forgejo appear under it.
    /// </summary>
    private const string HttpClientSource = "System.Net.Http";

    extension(WorkflowApplicationBuilder builder)
    {
        /// <summary>
        /// Enables OpenTelemetry for the builder.
        /// </summary>
        /// <param name="configure">Delegate for configuring OTEL.</param>
        /// <returns>The builder, for chaining.</returns>
        public WorkflowApplicationBuilder AddOpenTelemetry(Action<IOpenTelemetryBuilder>? configure = null)
        {
            var telemetry = builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .AddSource(RittenTelemetry.SourceName)
                    .AddSource(HttpClientSource)
                    .AddOtlpExporter());
            configure?.Invoke(telemetry);

            builder.Services.AddScoped<IWorkflowProgress, TracerProviderLifetime>();
            return builder;
        }
    }
}
