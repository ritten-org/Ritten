using NuGet.Versioning;

namespace Ritten.Releases;

/// <summary>
/// The version the repository is being staged for, and how it was arrived at.
/// </summary>
/// <param name="Version">The version the next release will carry.</param>
/// <param name="Bumped">Whether this moves the project off its declared version.</param>
/// <param name="Reason">Why this version, for the narrative.</param>
public sealed record PreparedRelease(NuGetVersion Version, bool Bumped, string Reason);
