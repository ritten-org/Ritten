using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Reporting.Sinks;
using Ritten.Runtimes;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Reporting;

/// <summary>
/// Posts a pending PR comment when the pipeline starts and publishes the final
/// build report to every registered sink when it finishes.
/// </summary>
internal class BuildReportPublisher(
    IPipelineLog log,
    IOptions<GitHubOptions> options,
    IBuildReport report,
    MarkdownReportRenderer renderer,
    ICommentService comments,
    IEnumerable<IReportSink> sinks
) : IProgressReporter
{
    /// <inheritdoc />
    public async Task OnPipelineStarted(Pipeline pipeline, CancellationToken cancellationToken)
    {
        if (!options.Value.IsPullRequest)
        {
            return;
        }

        try
        {
            await comments.CreateOrUpdate($"## ⏳ {options.Value.WorkflowName}\n\n{pipeline.Name} pipeline in progress…", cancellationToken);
        }
        catch (Exception ex)
        {
            log.Warning($"Failed to post the pending pull request comment: {ex.Message}");
            log.Verbose(ex.ToString());
        }
    }

    /// <inheritdoc />
    public Task OnStepStarted(IPipelineStep step, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnStepCompleted(IPipelineStep step, StepResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task OnPipelineCompleted(PipelineResult result, CancellationToken cancellationToken)
    {
        var markdown = renderer.Render(options.Value.WorkflowName, result.IsSuccess, report.Sections);
        foreach (var sink in sinks)
        {
            try
            {
                await sink.Publish(markdown, cancellationToken);
            }
            catch (Exception ex)
            {
                log.Warning($"Failed to publish the build report via {sink.GetType().Name}: {ex.Message}");
                log.Verbose(ex.ToString());
            }
        }
    }
}
