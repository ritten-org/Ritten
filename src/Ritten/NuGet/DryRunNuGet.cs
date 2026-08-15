using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.NuGet;

/// <summary>
/// Reports what would be published instead of publishing it.
/// Reads pass through, so version validation still runs against the real feed.
/// </summary>
internal class DryRunNuGet(IPipelineLog log, INuGet inner) : INuGet
{
    /// <inheritdoc />
    public Task<IReadOnlyList<NuGetVersion>> GetPublishedVersions(NuGetFeed feed, string packageId, CancellationToken cancellationToken = default) =>
        inner.GetPublishedVersions(feed, packageId, cancellationToken);

    /// <inheritdoc />
    public Task Push(NuGetFeed feed, IFile package, CancellationToken cancellationToken = default)
    {
        log.Status($"Would push {package.Name} to {feed.Url}.");
        return Task.CompletedTask;
    }
}
