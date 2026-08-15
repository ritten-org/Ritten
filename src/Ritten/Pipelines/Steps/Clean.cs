using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.Pipelines.Steps;

/// <summary>
/// Deletes the artifacts and temp directories so the pipeline starts from a clean slate.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="fileSystem">The file system.</param>
[Step("clean", StepKind.Work)]
public class Clean(IPipelineLog log, IOptions<PipelineOptions> options, IFileSystem fileSystem)
{
    /// <summary>
    /// Deletes the artifacts and temp directories.
    /// </summary>
    public StepResult Run()
    {
        var root = fileSystem.ProjectRoot;
        var deleted = new List<string>();
        foreach (var name in new[] { options.Value.ArtifactsDirectory, options.Value.TempDirectory })
        {
            var directory = root.GetDirectory(name);
            if (directory.Exists)
            {
                directory.Delete();
                deleted.Add(name);
            }
        }

        log.Detail(deleted.Count == 0 ? "Nothing to clean." : $"Deleted {string.Join(" and ", deleted)}.");
        return StepResult.Successful;
    }
}
