using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.Pipelines.Steps;

/// <summary>
/// Deletes the artifacts and temp directories so the pipeline starts from a clean slate.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="fileSystem">The file system.</param>
[Step("clean", StepKind.Work)]
public class Clean(IPipelineLog log, IFileSystem fileSystem)
{
    /// <summary>
    /// Deletes the artifacts and temp directories.
    /// </summary>
    public StepResult Run()
    {
        var deleted = new List<string>();
        foreach (var directory in new[] { fileSystem.Artifacts, fileSystem.Temp })
        {
            if (directory.Exists)
            {
                directory.Delete();
                deleted.Add(directory.Name);
            }
        }

        log.Detail(deleted.Count == 0 ? "Nothing to clean." : $"Deleted {string.Join(" and ", deleted)}.");
        return StepResult.Successful;
    }
}
