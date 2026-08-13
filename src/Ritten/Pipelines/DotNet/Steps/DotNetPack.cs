using System.ComponentModel;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Pipelines.NuGet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Packs the configured project into the artifacts directory.
/// Sets <see cref="PackResult"/> in pipeline state for <see cref="NuGetPush"/>.
/// </summary>
/// <param name="options">The pipeline's .NET options.</param>
/// <param name="pipeline">The pipeline's directory layout options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="dotnet">The dotnet client.</param>
[DisplayName("Pack NuGet Package")]
public class DotNetPack(IOptions<DotNetOptions> options, IOptions<PipelineOptions> pipeline, IPipelineContext context, IDotNet dotnet) : IPipelineStep
{
    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Pack(
            new PackArgs
            {
                Project = options.Value.ProjectFile,
                Configuration = options.Value.Configuration,
                NoBuild = true,
                Output = context.FileSystem.CurrentDirectory.GetDirectory(pipeline.Value.ArtifactsDirectory)
            },
            cancellationToken);

        context.State.Set(result);
    }
}
