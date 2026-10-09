using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Docker.Steps;

/// <summary>
/// Builds the images the component declares, from its own source.
/// </summary>
/// <param name="docker">The docker client.</param>
/// <param name="fileSystem">The workflow's file system.</param>
/// <param name="log">The run's log.</param>
[Step("build images", StepKind.Work)]
public class BuildImages(IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    /// <summary>
    /// Builds each declared image.
    /// </summary>
    /// <param name="images">The images the component declares.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(DockerImages images, CancellationToken cancellationToken = default)
    {
        var declared = images.Images;
        if (declared.Count == 0)
        {
            log.Detail("No images to build.");
            return StepResult.Successful;
        }

        foreach (var image in declared)
        {
            var context = fileSystem.ProjectRoot.GetDirectory(image.Context);
            if (!context.GetFile("Dockerfile").Exists)
            {
                return StepResult.Failed($"{context.AbsolutePath} has no Dockerfile.");
            }

            await docker.Build(context, image.Tag, ct: cancellationToken);
            log.Status($"Built {image.Tag}.");
        }

        return StepResult.Successful;
    }
}
