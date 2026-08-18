namespace Ritten.Releases;

/// <summary>
/// Whether one package's version is already on the feed.
/// </summary>
/// <param name="Name">The package ID.</param>
/// <param name="Published">Whether the package's version is already published.</param>
public sealed record PackagePublication(string Name, bool Published);
