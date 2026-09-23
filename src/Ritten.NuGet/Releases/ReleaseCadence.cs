namespace Ritten.Releases;

/// <summary>
/// When a merged change becomes a release, which decides what the version check must prove.
/// </summary>
public enum ReleaseCadence
{
    /// <summary>
    /// A maintainer decides when to release, so a merge may change the package without moving its version.
    /// </summary>
    Curated,

    /// <summary>
    /// Every merge that changes what ships is released, so a pull request that changes it must move the version too.
    /// </summary>
    Continuous
}
