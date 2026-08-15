namespace Ritten.Pipelines;

/// <summary>
/// How the project's version stands relative to what's published.
/// </summary>
public enum ReleaseStateKind
{
    /// <summary>
    /// The current version is unpublished and newer than any other published version.
    /// </summary>
    Releasable,

    /// <summary>
    /// The current version is the latest published one in its line, and new work accrues under <c>[Unreleased]</c> until a release is prepared.
    /// </summary>
    LatestInLine
}
