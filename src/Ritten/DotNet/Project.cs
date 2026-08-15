using NuGet.Versioning;

namespace Ritten.DotNet;

/// <summary>
/// The package information extracted from a .NET project file.
/// </summary>
public record Project
{
    /// <summary>
    /// The package ID of the project.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The package version of the project.
    /// </summary>
    public required NuGetVersion Version { get; init; }

    /// <summary>
    /// The repository's web URL.
    /// </summary>
    public string? Repository { get; init; }

    /// <summary>
    /// Whether this version is a prerelease.
    /// </summary>
    public bool IsPrerelease => Version.IsPrerelease;
}
