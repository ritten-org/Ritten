using NuGet.Versioning;

namespace Ritten.Releases;

/// <summary>
/// The version the caller named, rather than one derived from what the repository says.
/// </summary>
/// <param name="Version">The version asked for, or null when the caller named none.</param>
public sealed record RequestedVersion(NuGetVersion? Version)
{
    /// <summary>
    /// The caller named no version.
    /// </summary>
    public static RequestedVersion None { get; } = new((NuGetVersion?)null);
}
