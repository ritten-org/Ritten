using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.Pipelines;

/// <summary>
/// Deletes the artifacts and temp directories so the pipeline starts from a clean slate.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="fileSystem">The file system.</param>
public class Clean(IPipelineLog log, IOptions<PipelineOptions> options, IFileSystem fileSystem) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "clean";

    /// <inheritdoc />
    public Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        log.Detail("Cleaning temp and artifact directories.");
        var cd = fileSystem.ProjectRoot;
        cd.GetDirectory(options.Value.ArtifactsDirectory).Delete();
        cd.GetDirectory(options.Value.TempDirectory).Delete();
        return Task.FromResult(StepResult.Successful);
    }
}
