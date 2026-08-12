using Hamelin.Hooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Reporting.GitHub;

namespace Wolfe.Hamelin.Build.Reporting.Hooks;

/// <summary>
/// Posts a placeholder comment as soon as a pull request run starts, so the reader sees the
/// pipeline is underway; the final report replaces it in place.
/// </summary>
public class PendingCommentHook(
    ILogger<PendingCommentHook> logger,
    IOptions<GitHubOptions> options,
    IPullRequestCommentService comments
) : IPrePipelineHook
{
    public async Task PrePipeline(PrePipelineHookArgs args, CancellationToken cancellationToken = default)
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
            // Reporting must never fail the build.
            logger.LogWarning(ex, "Failed to post the pending pull request comment.");
        }
    }
}
