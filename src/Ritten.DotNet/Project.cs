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
    /// The project file this was read from, relative to the repository root, when known.
    /// </summary>
    public string ProjectFile { get; init; } = "";

    /// <summary>
    /// Whether the project packs as a .NET tool (<c>PackAsTool</c>).
    /// </summary>
    public bool IsTool { get; init; }

    /// <summary>
    /// The command the tool installs as (<c>ToolCommandName</c>), when the project names one.
    /// </summary>
    public string? ToolCommand { get; init; }

    /// <summary>
    /// The metadata a feed surfaces for the package: description, readme, and license.
    /// </summary>
    public PackageMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Whether this version is a prerelease.
    /// </summary>
    public bool IsPrerelease => Version.IsPrerelease;
}
