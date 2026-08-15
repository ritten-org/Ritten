using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Pipelines.NuGet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Packs the configured project into the artifacts directory.
/// Sets <see cref="PackResult"/> in pipeline state for <see cref="NugetPush"/>.
/// </summary>
/// <param name="options">The pipeline's .NET options.</param>
/// <param name="pipeline">The pipeline's directory layout options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="dotnet">The dotnet client.</param>
public class DotnetPack(IOptions<DotNetOptions> options, IOptions<PipelineOptions> pipeline, IFileSystem fileSystem, IPipelineState state, IDotNet dotnet) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "dotnet pack";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Pack(
            new PackArgs
            {
                Project = options.Value.ProjectFile,
                Configuration = options.Value.Configuration,
                NoBuild = true,
                Output = fileSystem.ProjectRoot.GetDirectory(pipeline.Value.ArtifactsDirectory)
            },
            cancellationToken);

        state.Set(result);
        return StepResult.Successful;
    }
}
