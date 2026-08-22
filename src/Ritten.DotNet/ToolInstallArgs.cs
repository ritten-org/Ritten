using NuGet.Versioning;
using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// The arguments for <c>dotnet tool install</c> and <c>dotnet tool update</c>.
/// </summary>
public record ToolInstallArgs
{
    /// <summary>
    /// The package ID of the tool.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Whether the tool belongs to the machine or to a repository's manifest.
    /// </summary>
    public required ToolScope Scope { get; init; }

    /// <summary>
    /// The exact version, or null for whatever the feed's latest is.
    /// </summary>
    public NuGetVersion? Version { get; init; }

    /// <summary>
    /// The directory holding the packed tool, if it shouldn't be installed from the feed.
    /// </summary>
    public IDirectory? Source { get; init; }
}
