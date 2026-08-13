using Hamelin.FileSystem;
using NuGet.Versioning;

namespace Ritten.NuGet;

/// <summary>
/// Exposes functionality for interacting with NuGet feeds.
/// </summary>
public interface INuGet
{
    /// <summary>
    /// Gets every published version of the given package, in ascending order.
    /// </summary>
    /// <param name="feed">The feed to query.</param>
    /// <param name="packageId">The ID of the package to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task<IReadOnlyList<NuGetVersion>> GetPublishedVersions(NuGetFeed feed, string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes the package to the feed, authenticating with <see cref="NuGetFeed.ApiKey"/> when set.
    /// Already-published versions are skipped rather than failing, so pushes are safe to rerun.
    /// </summary>
    /// <param name="feed">The feed to push to.</param>
    /// <param name="package">The .nupkg file to push.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task Push(NuGetFeed feed, IFile package, CancellationToken cancellationToken = default);
}
