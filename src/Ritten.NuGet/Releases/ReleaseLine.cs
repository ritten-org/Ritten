namespace Ritten.Releases;

/// <summary>
/// How published versions are grouped into release lines during checks.
/// </summary>
public enum ReleaseLine
{
    /// <summary>
    /// Each major version is its own line. Fixes can ship to an older major, but never to an older minor within one.
    /// </summary>
    Major,

    /// <summary>
    /// Each major.minor pair is its own line, for projects that treat the major number as a product version and make breaking changes in minors.
    /// </summary>
    Minor
}
