using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core;

namespace Ritten.GitHub;

/// <summary>
/// Posts a pending comment when the pipeline starts, so the pull request shows the run is underway
/// before any result exists. The final report replaces it via <see cref="GitHubCommentSink"/>.
/// </summary>
internal class PendingCommentReporter(
    IPipelineLog log,
    IOptions<GitHubActionsOptions> options,
    IOptions<RunContext> context,
    ICommentService comments
) : IProgressReporter
{
    /// <inheritdoc />
    public async Task OnPipelineStarted(PipelineJob job, CancellationToken cancellationToken)
    {
        if (!options.Value.IsPullRequest)
        {
            return;
        }

        try
        {
            await comments.CreateOrUpdate($"## ⏳ {context.Value.Title}\n\n{job.Name} job in progress…", cancellationToken);
        }
        catch (Exception ex)
        {
            log.Warning("Failed to post the pending pull request comment.", ex);
        }
    }

    /// <inheritdoc />
    public Task OnStepStarted(Step step, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnPipelineCompleted(PipelineResult result, CancellationToken cancellationToken) => Task.CompletedTask;
}
