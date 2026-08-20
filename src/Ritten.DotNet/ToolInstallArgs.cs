using NuGet.Versioning;
using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// The arguments for <c>dotnet tool install</c>.
/// </summary>
public record ToolInstallArgs
{
    /// <summary>
    /// The package ID of the tool to install.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// The exact version to install.
    /// </summary>
    public required NuGetVersion Version { get; init; }

    /// <summary>
    /// The directory holding the packed tool, used as the only package source.
    /// </summary>
    public required IDirectory Source { get; init; }
}
