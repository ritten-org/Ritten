using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.Pipelines;

/// <summary>
/// Deletes the artifacts and temp directories so the pipeline starts from a clean slate.
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="fileSystem">The file system.</param>
public class CleanDirectories(ILogger<CleanDirectories> logger, IOptions<PipelineOptions> options, IFileSystem fileSystem) : IPipelineStep
{
    /// <inheritdoc />
    public Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cleaning temp and artifact directories.");
        var cd = fileSystem.CurrentDirectory;
        cd.GetDirectory(options.Value.ArtifactsDirectory).Delete();
        cd.GetDirectory(options.Value.TempDirectory).Delete();
        return Task.FromResult(StepResult.Successful);
    }
}
