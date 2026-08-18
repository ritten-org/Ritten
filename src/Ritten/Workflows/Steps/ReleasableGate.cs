using Ritten.Contracts;
using Ritten.Releases;

namespace Ritten.Workflows.Steps;

/// <summary>
/// Ends a deploy successfully when this version has already been released.
/// </summary>
/// <param name="log">The workflow log.</param>
[Step("releasable gate", StepKind.Gate)]
public class ReleasableGate(IWorkflowLog log)
{
    /// <summary>
    /// Stops the job, successfully, when there is nothing to release.
    /// </summary>
    /// <param name="release">The release state determined against the feed.</param>
    public StepResult Run(ReleaseState release)
    {
        if (release.Published)
        {
            log.Skipped($"Version {release.LatestVersionInLine} is already published; nothing to deploy.");
            return StepResult.NothingToDo;
        }

        return StepResult.Successful;
    }
}
