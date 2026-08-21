namespace Ritten.Releases;

/// <summary>
/// What a release does to what already shipped, which is what sizes its version.
/// </summary>
public enum ReleaseKind
{
    /// <summary>
    /// There is nothing to release.
    /// </summary>
    None,

    /// <summary>
    /// It only fixes what already shipped.
    /// </summary>
    Fixes,

    /// <summary>
    /// It adds to what already shipped, leaving what was there alone.
    /// </summary>
    Features,

    /// <summary>
    /// It changes or removes what already shipped, so a caller can break on it.
    /// </summary>
    Breaking
}
