using Hamelin.Hooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Reporting.GitHub;
using Wolfe.Hamelin.Build.Reporting.Sinks;

namespace Wolfe.Hamelin.Build.Reporting.Hooks;

/// <summary>
/// Renders the accumulated report once the pipeline has finished and publishes it to every
/// registered sink.
/// </summary>
public class PublishReportHook(
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
