using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Reporting.Sinks;

namespace Ritten.Reporting;

/// <summary>
/// Publishes the final build report to every registered sink when the pipeline finishes.
/// </summary>
internal class BuildReportPublisher(
    IPipelineLog log,
    IOptions<RunContext> context,
    IBuildReport report,
    MarkdownReportRenderer renderer,
    IEnumerable<IReportSink> sinks
) : IProgressReporter
{
    /// <inheritdoc />
    public Task OnPipelineStarted(PipelineJob job, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnStepStarted(Step step, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task OnPipelineCompleted(PipelineResult result, CancellationToken cancellationToken)
    {
        var markdown = renderer.Render(context.Value.Title, result.IsSuccess, report.Sections, result.FailedStep);
        foreach (var sink in sinks)
        {
            try
            {
                await sink.Publish(markdown, cancellationToken);
            }
            catch (Exception ex)
            {
                log.Warning($"Failed to publish the build report via {sink.GetType().Name}", ex);
            }
        }
    }
}
