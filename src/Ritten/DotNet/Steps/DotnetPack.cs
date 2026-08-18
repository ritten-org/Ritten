using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Packs every shipped package into the artifacts directory.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's .NET options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("dotnet pack", StepKind.Work)]
public class DotnetPack(IWorkflowLog log, IOptions<DotNetOptions> options, IFileSystem fileSystem, IDotNet dotnet)
{
    /// <summary>
    /// Packs every shipped package.
    /// </summary>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<PackResult>> Run(PackageSet packages, CancellationToken cancellationToken = default)
    {
        // Each pack reports the output directory's state, so the last result lists them all.
        PackResult? result = null;
        foreach (var package in packages.Packages)
        {
            result = await dotnet.Pack(
                new PackArgs
                {
                    Project = package.ProjectFile,
                    Configuration = options.Value.Configuration,
                    NoBuild = true,
                    Output = fileSystem.Artifacts
                },
                cancellationToken);
        }

        if (result is null)
        {
            return StepResult.Failed("There are no packages to pack.");
        }

        foreach (var package in result.Packages)
        {
            log.Detail($"Packed {package.Name}.");
        }

        return result;
    }
}
