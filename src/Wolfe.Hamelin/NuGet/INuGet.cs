using NuGet.Versioning;

namespace Wolfe.Hamelin.NuGet;

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
}
