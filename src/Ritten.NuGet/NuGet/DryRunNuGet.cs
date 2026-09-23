using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.NuGet;

/// <summary>
/// Reports what would be published instead of publishing it.
/// Reads pass through, so version checks still runs against the real feed.
/// </summary>
internal class DryRunNuGet(IWorkflowLog log, INuGet inner) : INuGet
{
    /// <inheritdoc />
    public Task<IReadOnlyList<NuGetVersion>> GetPublishedVersions(NuGetFeed feed, string packageId, CancellationToken cancellationToken = default) =>
        inner.GetPublishedVersions(feed, packageId, cancellationToken);

    /// <inheritdoc />
    public Task Push(NuGetFeed feed, IFile package, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would push {package.Name} to {feed.Url}.");
        return Task.CompletedTask;
    }
}
