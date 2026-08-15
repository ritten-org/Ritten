using Ritten.Contracts;

namespace Ritten.Runtimes.GitHubActions;

/// <summary>
/// Reports what would be released instead of releasing it.
/// </summary>
internal class DryRunReleaseService(IPipelineLog log) : IReleaseService
{
    /// <inheritdoc />
    public Task<bool> Exists(string tag, CancellationToken cancellationToken = default)
    {
        // Nothing is being created, so nothing it created is in the way.
        log.Detail($"Would check whether a GitHub release exists for tag {tag}.");
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task Create(string tag, string title, string notes, CancellationToken cancellationToken = default)
    {
        log.Status($"Would create the GitHub release {title} for tag {tag}.");
        return Task.CompletedTask;
    }
}
