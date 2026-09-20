using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Docker.Steps;

/// <summary>
/// Builds the images the component declares, from its own source.
/// </summary>
/// <param name="options">The component's docker options.</param>
/// <param name="docker">The docker client.</param>
/// <param name="fileSystem">The workflow's file system.</param>
/// <param name="log">The run's log.</param>
[Step("build images", StepKind.Work)]
public class BuildImages(IOptions<DockerOptions> options, IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    /// <summary>
    /// Builds each declared image.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var images = options.Value.Images;
        if (images.Count == 0)
        {
            log.Detail("No images to build.");
            return StepResult.Successful;
        }

        foreach (var image in images)
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
