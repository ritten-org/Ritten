using Ritten.Contracts;

namespace Ritten.Pipelines;

/// <summary>
/// Ends a deploy successfully when this version has already been released.
/// </summary>
/// <param name="log">The pipeline log.</param>
public class ReleasableGate(IPipelineLog log) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "releasable gate";

    /// <inheritdoc />
    public StepKind Kind => StepKind.Gate;

    /// <summary>
    /// Stops the job, successfully, when there is nothing to release.
    /// </summary>
    /// <param name="release">The release state determined against the feed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public Task<StepResult> Run(ReleaseState release, CancellationToken cancellationToken = default)
    {
        if (release.Kind == ReleaseStateKind.LatestInLine)
        {
            log.Skipped($"Version {release.LatestVersionInLine} is already published; nothing to deploy.");
            return Task.FromResult(StepResult.NothingToDo);
        }

        return Task.FromResult(StepResult.Successful);
    }
}
