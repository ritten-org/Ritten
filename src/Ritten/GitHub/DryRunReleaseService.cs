using Ritten.Contracts;

namespace Ritten.GitHub;

/// <summary>
/// Reports what would be released instead of releasing it.
/// </summary>
internal class DryRunReleaseService(IPipelineLog log) : IReleaseService
{
    /// <inheritdoc />
    public Task<bool> Exists(RepositoryPath repository, string tag, CancellationToken cancellationToken = default)
    {
        // Nothing is being created, so nothing it created is in the way.
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task Create(RepositoryPath repository, string tag, string title, string notes, bool makeLatest = true, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would create the GitHub release {title} for tag {tag} in {repository}, {(makeLatest ? "marked latest" : "not marked latest")}.");
        return Task.CompletedTask;
    }
}
