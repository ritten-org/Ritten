using NuGet.Versioning;

namespace Ritten.Init;

/// <summary>
/// The tool a repository is being set up to run.
/// </summary>
/// <param name="PackageId">The tool's package ID, as the tool manifest pins it.</param>
/// <param name="Command">The command the tool is invoked as, as in <c>dotnet ritten</c>.</param>
/// <param name="Version">The version to pin.</param>
public sealed record ToolPin(string PackageId, string Command, NuGetVersion Version);
