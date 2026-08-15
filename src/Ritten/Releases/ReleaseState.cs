using NuGet.Versioning;

namespace Ritten.Releases;

/// <summary>
/// Where the project's version stands against the feed.
/// </summary>
/// <param name="Published">Whether the version is already on the feed.</param>
/// <param name="LatestInLine">Whether the version is at or ahead of its line's tip — nothing published on its line is newer.</param>
/// <param name="LatestVersionInLine">The latest published version on the project's own release line, or <c>null</c> when the line is new.</param>
/// <param name="LatestVersion">The latest published version overall, or <c>null</c> when nothing has been published.</param>
public sealed record ReleaseState(bool Published, bool LatestInLine, NuGetVersion? LatestVersionInLine, NuGetVersion? LatestVersion)
{
    /// <summary>
    /// Whether the version's release line is the latest line.
    /// </summary>
    public bool OnLatestLine => LatestVersionInLine == LatestVersion;
}
