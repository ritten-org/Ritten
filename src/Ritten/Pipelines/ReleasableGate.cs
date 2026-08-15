using Ritten.Contracts;

namespace Ritten.Pipelines;

/// <summary>
/// Ends a deploy successfully when this version has already been released.
/// </summary>
/// <param name="state">The pipeline state.</param>
/// <param name="log">The pipeline log.</param>
public class ReleasableGate(IPipelineState state, IPipelineLog log) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "releasable gate";

    /// <inheritdoc />
    public Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (state.Get<ReleaseState>() is not { } release)
        {
            return Task.FromResult(StepResult.Failed("Release state not found in state."));
        }

        if (release.Kind == ReleaseStateKind.LatestInLine)
        {
            log.Skipped($"Version {release.LatestVersionInLine} is already published; nothing to deploy.");
            return Task.FromResult(StepResult.NothingToDo);
        }

        return Task.FromResult(StepResult.Successful);
    }
}
