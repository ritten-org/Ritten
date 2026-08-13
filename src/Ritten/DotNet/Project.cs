using NuGet.Versioning;

namespace Ritten.DotNet;

/// <summary>
/// The package information extracted from a .NET project file.
/// </summary>
public class Project
{
    /// <summary>
    /// The package ID of the project.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The package version of the project.
    /// </summary>
    public required NuGetVersion Version { get; init; }
}
