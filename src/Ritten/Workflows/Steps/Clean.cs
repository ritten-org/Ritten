using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Workflows.Steps;

/// <summary>
/// Deletes the artifacts and temp directories so the workflow starts from a clean slate.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
[Step("clean", StepKind.Work)]
public class Clean(IWorkflowLog log, IFileSystem fileSystem)
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
