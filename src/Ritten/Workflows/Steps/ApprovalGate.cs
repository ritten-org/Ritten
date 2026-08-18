using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.Workflows.Steps;

/// <summary>
/// Stops and asks before anything irreversible happens.
/// </summary>
/// <param name="job">The job being run.</param>
/// <param name="log">The workflow log.</param>
/// <param name="prompt">The prompt used to ask.</param>
[Step("approval gate", StepKind.Gate)]
public class ApprovalGate(WorkflowJob job, IWorkflowLog log, IWorkflowPrompt prompt)
{
    /// <summary>
    /// Asks for approval, unless the run already carries it.
    /// </summary>
    /// <param name="project">The project being released, when one has been read, for the confirmation message.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(Project? project, CancellationToken cancellationToken = default)
    {
        if (job.DryRun)
        {
            log.Skipped("Nothing to approve: this is a dry run.");
            return StepResult.Successful;
        }

        if (job.AutoApprove)
        {
            log.Skipped($"Approved automatically by --{WorkflowArguments.AutoApprove}.");
            return StepResult.Successful;
        }

        if (!prompt.IsInteractive)
        {
            // Hanging on a build agent waiting for a person is worse than refusing to start.
            return StepResult.Failed(
                $"The {job.Name} job needs approval, and there's no terminal to ask at. " +
                $"Pass --{WorkflowArguments.AutoApprove} to approve it up front.");
        }

        var release = project is not null ? $"{project.Name} {project.Version}" : job.Name;
        if (!await prompt.Confirm($"About to release {release}. This cannot be undone.", cancellationToken))
        {
            return StepResult.Failed($"The {job.Name} job was not approved.");
        }

        return StepResult.Successful;
    }
}
