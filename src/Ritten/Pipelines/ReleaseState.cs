using NuGet.Versioning;

namespace Ritten.Pipelines;

/// <summary>
/// The release state of the project, stored in pipeline state by <see cref="NuGet.NugetValidate"/> for the steps that act on it.
/// </summary>
/// <param name="Kind">Which state the project is in.</param>
/// <param name="LatestVersionInLine">The latest published version on the project's own release line, or <c>null</c> when the line is new.</param>
/// <param name="LatestVersion">The latest published version overall, or <c>null</c> when nothing has been published.</param>
public sealed record ReleaseState(ReleaseStateKind Kind, NuGetVersion? LatestVersionInLine, NuGetVersion? LatestVersion)
{
    /// <summary>
    /// The version is unpublished and ahead of everything published on its release line.
    /// </summary>
    /// <param name="latestInLine">The latest published version on the project's own release line, or <c>null</c> when the line is new.</param>
    /// <param name="latestPublished">The latest published version overall, or <c>null</c> when nothing has been published.</param>
    public static ReleaseState Releasable(NuGetVersion? latestInLine, NuGetVersion? latestPublished) => new(ReleaseStateKind.Releasable, latestInLine, latestPublished);

    /// <summary>
    /// The current version is the latest published one on its release line, and new work accrues under <c>[Unreleased]</c> until a release is prepared.
    /// </summary>
    /// <param name="latestInLine">The latest published version on the project's release line, which is the project's own.</param>
    /// <param name="latestPublished">The latest published version overall.</param>
    public static ReleaseState LatestInLine(NuGetVersion latestInLine, NuGetVersion latestPublished) => new(ReleaseStateKind.LatestInLine, latestInLine, latestPublished);
}
