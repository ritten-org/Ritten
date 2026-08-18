using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Packs the configured project into the artifacts directory.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's .NET options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("dotnet pack", StepKind.Work)]
public class DotnetPack(IWorkflowLog log, IOptions<DotNetOptions> options, IFileSystem fileSystem, IDotNet dotnet)
{
    /// <summary>
    /// Packs the configured project.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<PackResult>> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Pack(
            new PackArgs
            {
                Project = options.Value.ProjectFile,
                Configuration = options.Value.Configuration,
                NoBuild = true,
                Output = fileSystem.Artifacts
            },
            cancellationToken);

        foreach (var package in result.Packages)
        {
            log.Detail($"Packed {package.Name}.");
        }

        return result;
    }
}
