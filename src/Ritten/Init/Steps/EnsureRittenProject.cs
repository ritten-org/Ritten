using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Init.Steps;

/// <summary>
/// Makes sure the repository's project file declares the workflow it runs, and what it builds.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="files">The project file client.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="project">Where the project file is, and whether it was there to read.</param>
/// <param name="workflow">The workflow being set up, and how it was chosen.</param>
/// <param name="job">The job being run.</param>
/// <param name="prompt">The prompt used to confirm a workflow nobody declared.</param>
[Step("ensure project", StepKind.Work)]
public class EnsureRittenProject(
    IWorkflowLog log,
    IProjectFiles files,
    IFileSystem fileSystem,
    RittenProject project,
    SelectedWorkflow workflow,
    WorkflowJob job,
    IWorkflowPrompt prompt
)
{
    /// <summary>
    /// Writes down what the repository runs, filling in only what it doesn't already say.
    /// </summary>
    /// <param name="found">What the repository builds (see <see cref="FindProjects"/>).</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(DiscoveredProjects found, CancellationToken ct = default)
    {
        if (await Confirmed(ct) is { IsFailure: true } refused)
        {
            return refused;
        }

        var file = fileSystem.ProjectRoot.GetFile(project.FileName);
        var read = await files.Read(file, ct);
        if (read.IsError)
        {
            return StepResult.Failed(read.Errors);
        }

        var document = read.Value;
        List<string> written = [];

        if (document.Workflow is null)
        {
            document.Workflow = workflow.Workflow.Name;
            written.Add($"workflow: {workflow.Workflow.Name}");
        }

        if (!document.Has("build.project") && !document.Has("build.projects"))
        {
            // One package is spelled singular and several plural.
            switch (found.Shipped)
            {
                case [var only]:
                    document.Set("build.project", only);
                    written.Add($"build.project: {only}");
                    break;
                case { Count: > 1 }:
                    document.Set("build.projects", found.Shipped);
                    written.Add($"build.projects: {found.Shipped.Count} projects");
                    break;
                default:
                    log.Warning($"No projects found, so {project.FileName} doesn't say what to build. Add 'build.project' once there is one.");
                    break;
            }
        }

        if (written.Count == 0)
        {
            log.Skipped($"{project.FileName} already says what it runs.");
            return StepResult.Successful;
        }

        await files.Write(file, document, ct);
        log.Detail($"{project.FileName}: {string.Join(", ", written)}.");
        return StepResult.Successful;
    }

    /// <summary>
    /// A workflow nobody declared was recognized from what's in the repository, and a guess about
    /// what a repository is for is worth confirming before it's written into the repository.
    /// </summary>
    private async Task<StepResult> Confirmed(CancellationToken ct)
    {
        if (workflow.Recognised is not { } reason)
        {
            return StepResult.Successful;
        }

        log.Detail($"Nothing declares a workflow yet. This looks like a {workflow.Workflow.Label} repository: {reason}.");

        if (job.DryRun)
        {
            return StepResult.Successful;
        }

        if (job.AutoApprove)
        {
            log.Skipped($"Approved automatically by --{WorkflowArguments.AutoApprove}.");
            return StepResult.Successful;
        }

        if (!prompt.IsInteractive)
        {
            return StepResult.Failed(
                $"This looks like a {workflow.Workflow.Label} repository, and there's no terminal to confirm that at. " +
                $"Pass --{WorkflowArguments.Workflow} to name the workflow to set up.");
        }

        return await prompt.Confirm($"Set this repository up for the {workflow.Workflow.Label} workflow?", ct)
            ? StepResult.Successful
            : StepResult.Failed($"Nothing was set up. Pass --{WorkflowArguments.Workflow} to name a different workflow.");
    }
}
