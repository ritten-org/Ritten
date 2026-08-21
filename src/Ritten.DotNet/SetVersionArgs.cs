using NuGet.Versioning;

namespace Ritten.DotNet;

/// <summary>
/// The arguments for writing a new version into the files that declare it.
/// </summary>
public record SetVersionArgs
{
    /// <summary>
    /// The project files the repository ships, relative to the project root.
    /// </summary>
    public required IReadOnlyList<string> Projects { get; init; }

    /// <summary>
    /// The version the projects currently evaluate to.
    /// </summary>
    public required NuGetVersion Current { get; init; }

    /// <summary>
    /// The version to write.
    /// </summary>
    public required NuGetVersion Version { get; init; }
}
