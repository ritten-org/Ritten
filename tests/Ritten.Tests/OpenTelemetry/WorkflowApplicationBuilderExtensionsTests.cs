using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.OpenTelemetry;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.OpenTelemetry;

public class WorkflowApplicationBuilderExtensionsTests
{
    // Nothing listens on the discard port: the exporter's attempt fails at once, as against a collector that is down.
    private static readonly Func<string, string?> WithEndpoint =
        name => name == "OTEL_EXPORTER_OTLP_ENDPOINT" ? "http://127.0.0.1:9" : null;

    [Fact]
    public async Task AddOpenTelemetry_ExportsTheRunsTrace_WithTheHostsResource()
    {
        var exporter = new CapturingExporter();
        var application = WorkflowApplication.CreateBuilder().AddOpenTelemetry(telemetry => telemetry
            .ConfigureResource(resource => resource.AddAttributes([new("lab.component", "grafana")]))
            .WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(exporter))));

        await Run(application, "export", WithEndpoint);

        var job = exporter.Exported.Single(span => span.DisplayName == "Test export");
        exporter.Exported.Where(span => span.TraceId == job.TraceId && span.ParentSpanId == job.SpanId)
            .ShouldHaveSingleItem().DisplayName.ShouldBe("probe");
        exporter.Resource.ShouldNotBeNull().Attributes.ShouldContain(new KeyValuePair<string, object>("lab.component", "grafana"));
    }

    [Fact]
    public async Task AddOpenTelemetry_BuildsTheProviderFromTheApplicationsServices_SoADetectorReachesThem()
    {
        // The resource describes what the application is, so a detector reads what the host registered.
        var exporter = new CapturingExporter();
        var application = WorkflowApplication.CreateBuilder().AddOpenTelemetry(telemetry => telemetry
            .ConfigureResource(resource => resource.AddDetector(services => new ComponentDetector(services.GetRequiredService<Component>())))
            .WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(exporter))));
        application.Services.AddSingleton(new Component("grafana"));

        await Run(application, "detect", WithEndpoint);

        exporter.Resource.ShouldNotBeNull().Attributes.ShouldContain(new KeyValuePair<string, object>("component", "grafana"));
    }

    [Fact]
    public async Task AddOpenTelemetry_BuildsNothingWithoutAnEndpoint()
    {
        var exporter = new CapturingExporter();
        var application = WorkflowApplication.CreateBuilder().AddOpenTelemetry(telemetry => telemetry
            .WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(exporter))));

        await Run(application, "quiet", _ => null);

        exporter.Exported.ShouldNotContain(span => span.DisplayName == "Test quiet");
        exporter.Resource.ShouldBeNull();
    }

    private static async Task Run(WorkflowApplicationBuilder builder, string job, Func<string, string?> environment)
    {
        builder.Workflows.Add(new TestWorkflow(jobs: [new TestJob(job, steps: [Step.FromType<ProbeStep>()])]));
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.Services.AddSingleton(new StepProbe());

        using var application = builder.Build(environment).Value.ShouldNotBeNull();
        (await application.Run(job)).ShouldBe(ExitCode.Success);
    }

    /// <summary>
    /// Keeps what it is given, and the resource of the provider that gave it.
    /// </summary>
    private sealed class CapturingExporter : BaseExporter<Activity>
    {
        private readonly List<Activity> _exported = [];

        public IReadOnlyList<Activity> Exported
        {
            get
            {
                lock (_exported)
                {
                    return [.. _exported];
                }
            }
        }

        public Resource? Resource { get; private set; }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            Resource ??= ParentProvider?.GetResource();
            lock (_exported)
            {
                foreach (var activity in batch)
                {
                    _exported.Add(activity);
                }
            }

            return ExportResult.Success;
        }
    }

    private sealed record Component(string Name);

    private sealed class ComponentDetector(Component component) : IResourceDetector
    {
        public Resource Detect() => new([new("component", component.Name)]);
    }
}
