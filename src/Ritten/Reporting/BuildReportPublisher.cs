using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.GitHub;
using Ritten.Reporting.Sinks;

namespace Ritten.Reporting;

/// <summary>
/// Posts a pending PR comment when the pipeline starts and publishes the final
/// build report to every registered sink when it finishes.
/// </summary>
internal class BuildReportPublisher(
    ILogger<BuildReportPublisher> logger,
    IOptions<GitHubOptions> options,
    IBuildReport report,
    MarkdownReportRenderer renderer,
    ICommentService comments,
    IEnumerable<IReportSink> sinks
) : IProgressReporter
{
    /// <inheritdoc />
    public async Task OnPipelineStarted(CancellationToken cancellationToken)
    {
        if (!options.Value.IsPullRequest)
        {
            return;
        }

        try
        {
            await comments.CreateOrUpdate($"## ⏳ {options.Value.WorkflowName}\n\nRun in progress…", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to post the pending pull request comment.");
        }
    }

    /// <inheritdoc />
    public Task OnStepStarted(string stepName, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnStepCompleted(StepResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task OnPipelineCompleted(int exitCode, PipelineResult result, CancellationToken cancellationToken)
    {
        var succeeded = exitCode == 0;
        var markdown = renderer.Render(options.Value.WorkflowName, succeeded, report.Sections);

        foreach (var sink in sinks)
        {
            try
            {
                await sink.Publish(markdown, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish the build report via {Sink}.", sink.GetType().Name);
            }
        }
    }
}
