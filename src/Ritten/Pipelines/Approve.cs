using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Pipelines;

/// <summary>
/// Stops and asks before anything irreversible happens.
/// </summary>
/// <param name="job">The job being run.</param>
/// <param name="log">The pipeline log.</param>
/// <param name="prompt">The prompt used to ask.</param>
/// <param name="state">The pipeline state.</param>
public class Approve(PipelineJob job, IPipelineLog log, IPipelinePrompt prompt, IPipelineState state) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "request approval";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (job.DryRun)
        {
            log.Skipped("Nothing to approve: this is a dry run.");
            return StepResult.Successful;
        }

        if (job.AutoApprove)
        {
            log.Skipped($"Approved automatically by --{PipelineArguments.AutoApprove}.");
            return StepResult.Successful;
        }

        if (!prompt.IsInteractive)
        {
            // Hanging on a build agent waiting for a person is worse than refusing to start.
            return StepResult.Failed(
                $"The {job.Name} job needs approval, and there's no terminal to ask at. " +
                $"Pass --{PipelineArguments.AutoApprove} to approve it up front.");
        }

        var release = state.Get<Project>() is { } project ? $"{project.Name} {project.Version}" : job.Name;
        if (!await prompt.Confirm($"About to release {release}. This cannot be undone.", cancellationToken))
        {
            return StepResult.Failed($"The {job.Name} job was not approved.");
        }

        return StepResult.Successful;
    }
}
