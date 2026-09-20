using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Docker.Steps;

/// <summary>
/// Converges the component's stack: creates or recreates what the compose file declares, and
/// removes what it no longer does.
/// </summary>
/// <param name="docker">The docker client.</param>
/// <param name="fileSystem">The workflow's file system.</param>
/// <param name="job">The job being run.</param>
/// <param name="log">The run's log.</param>
[Step("compose up", StepKind.Publish)]
public class ComposeUp(IDocker docker, IFileSystem fileSystem, WorkflowJob job, IWorkflowLog log)
{
    /// <summary>
    /// Converges the stack.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        await docker.ComposeUp(fileSystem.ProjectRoot, ct: cancellationToken);
        if (!job.DryRun)
        {
            // The rehearsal has already said what it would do; saying it again here would be
            // the job claiming it happened.
            log.Status("Converged the stack.");
        }

        return StepResult.Successful;
    }
}
