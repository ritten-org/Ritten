using Ritten.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ritten.Pipelines;

/// <summary>
/// Deletes the artifacts and temp directories so the pipeline starts from a clean slate.
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="context">The pipeline context.</param>
public class CleanDirectories(ILogger<CleanDirectories> logger, IOptions<PipelineOptions> options, IPipelineContext context) : IPipelineStep
{
    /// <inheritdoc />
    public Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cleaning temp and artifact directories.");
        var cd = context.FileSystem.CurrentDirectory;
        cd.GetDirectory(options.Value.ArtifactsDirectory).Delete();
        cd.GetDirectory(options.Value.TempDirectory).Delete();
        return Task.FromResult(StepResult.Successful);
    }
}
