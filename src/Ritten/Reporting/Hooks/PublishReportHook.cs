using Hamelin.Hooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.GitHub;
using Ritten.Reporting.Sinks;

namespace Ritten.Reporting.Hooks;

/// <summary>
/// Renders the accumulated report once the pipeline has finished and publishes it to every registered sink.
/// </summary>
internal class PublishReportHook(
    ILogger<PublishReportHook> logger,
    IOptions<GitHubOptions> options,
    IBuildReport report,
    MarkdownReportRenderer renderer,
    IEnumerable<IReportSink> sinks
) : IPostPipelineHook
{
    public async Task PostPipeline(PostPipelineHookArgs args, CancellationToken cancellationToken = default)
    {
        var succeeded = args.ExitCode == 0;
        var markdown = renderer.Render(options.Value.WorkflowName, succeeded, report.Sections);

        foreach (var sink in sinks)
        {
            try
            {
                await sink.Publish(markdown, cancellationToken);
            }
            catch (Exception ex)
            {
                // Reporting must never fail the build.
                logger.LogWarning(ex, "Failed to publish the build report via {Sink}.", sink.GetType().Name);
            }
        }
    }
}
