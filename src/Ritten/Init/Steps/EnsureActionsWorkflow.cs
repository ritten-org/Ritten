using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Engine.Workflows;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.Reporting;

namespace Ritten.Init.Steps;

/// <summary>
/// Makes sure the repository's GitHub Actions workflow runs the jobs that guard and ship a change.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="actions">The GitHub Actions workflow client.</param>
/// <param name="git">The git client, for the root GitHub reads workflows from.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="options">The workflow's .NET options, for the project the repository ships.</param>
/// <param name="workflow">The workflow being set up, whose jobs the file runs.</param>
/// <param name="tool">The tool the jobs run.</param>
[Step("ensure actions workflow", StepKind.Work)]
public class EnsureActionsWorkflow(
    IWorkflowLog log,
    IActionsWorkflows actions,
    IGit git,
    IFileSystem fileSystem,
    IOptions<DotNetOptions> options,
    SelectedWorkflow workflow,
    ToolPin tool
)
{
    /// <summary>
    /// Writes the workflow's automated jobs into the repository's Actions workflow.
    /// </summary>
    /// <param name="found">What the repository builds (see <see cref="DotNet.Steps.FindProjects"/>).</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(DiscoveredProjects found, CancellationToken ct = default)
    {
        if (await git.RepositoryRoot(ct) is not { } root)
        {
            log.Warning("This isn't a git repository, so there's nowhere GitHub Actions would read a workflow from.");
            return StepResult.Successful;
        }

        List<IJob> jobs = [.. ActionsWorkflowTemplate.Automated(workflow.Workflow)];
        if (jobs.Count == 0)
        {
            log.Skipped($"The {workflow.Workflow.Label} workflow has no jobs worth running on GitHub Actions.");
            return StepResult.Successful;
        }

        // A repository of several projects runs each one's jobs from its own directory, and gets
        // one workflow file each: same jobs, different working directory, and the project's own
        // name on each.
        var directory = Directory(root);
        var name = Named(found);
        var file = await Ours(root, directory, ct) ?? await Free(root, directory, name, ct);

        var read = file.Exists ? await actions.Read(file, ct) : actions.Parse(ActionsWorkflowTemplate.Document(name));
        if (read.IsError)
        {
            return StepResult.Failed(read.Errors);
        }

        var globalJson = GlobalJson(root, directory);
        if (globalJson is null)
        {
            log.Warning("There's no global.json, so the workflow doesn't pin an SDK version. Add one, then run init again.");
        }

        var ghaWorkflow = read.Value;
        var before = ghaWorkflow.Text;
        foreach (var job in jobs)
        {
            foreach (var (trigger, block) in ActionsWorkflowTemplate.Triggers(job))
            {
                ghaWorkflow = ghaWorkflow.WithTrigger(trigger, block);
            }

            ghaWorkflow = ghaWorkflow.WithJob(job.Name, ActionsWorkflowTemplate.Job(job, tool, directory, globalJson));
        }

        if (ghaWorkflow.Text == before)
        {
            log.Skipped($"{file.Name} already runs {Named(jobs)}.");
            return StepResult.Successful;
        }

        await actions.Write(file, ghaWorkflow, ct);
        log.Detail($"{file.Name}: {Named(jobs)}.");
        return StepResult.Successful;
    }

    /// <summary>
    /// The workflow file that already runs this project's jobs, wherever it is and whatever it's
    /// called. A file that doesn't parse is somebody else's problem, not a reason to stop.
    /// </summary>
    private async Task<IFile?> Ours(IDirectory root, string? directory, CancellationToken ct)
    {
        foreach (var candidate in actions.Files(root))
        {
            var read = await actions.Read(candidate, ct);
            if (read.IsError)
            {
                log.Verbose($"Skipped {candidate.Name}: {read.Errors[0].Message}");
                continue;
            }

            if (Runs(read.Value, directory))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the workflow already runs this tool for this project. The working directory is
    /// what tells one project's workflow from another's in a repository of several.
    /// </summary>
    private bool Runs(ActionsWorkflow workflow, string? directory) => workflow.Jobs
        .SelectMany(job => job.Steps)
        .Any(step => step.Run.Contains($"dotnet {tool.Command} ", StringComparison.Ordinal)
                     && (step.WorkingDirectory ?? workflow.WorkingDirectory) == directory);

    /// <summary>
    /// Where the project is in the repository, or null when it is the repository.
    /// </summary>
    private string? Directory(IDirectory root)
    {
        var relative = root.RelativePath(fileSystem.ProjectRoot);
        return relative is "." or "" ? null : relative;
    }

    /// <summary>
    /// The SDK version file the workflow should point at: the project's own when it pins one, the
    /// repository's otherwise, and nothing at all when neither does.
    /// </summary>
    private static string? GlobalJson(IDirectory root, string? directory)
    {
        var project = directory is null ? null : $"{directory}/global.json";
        return project is not null && root.GetFile(project).Exists ? project
            : root.GetFile("global.json").Exists ? "global.json"
            : null;
    }

    /// <summary>
    /// What the Actions tab calls the workflow: the project it builds. A workflow named for the
    /// tool that wrote it would be the same name in every repository, and the same name twice in
    /// a repository of several projects — and the name is what keeps each project's runs, and
    /// each project's pull request comment, its own.
    /// </summary>
    private string Named(DiscoveredProjects found)
    {
        // What the project file declares, when it declares one: the first project is the face of
        // whatever the repository ships. A repository that declares nothing yet is read off disk,
        // and one with no projects at all is named for where it is.
        var shipped = options.Value.ProjectFile is { Length: > 0 } declared ? declared : found.Shipped.FirstOrDefault();
        return Path.GetFileNameWithoutExtension(shipped) is { Length: > 0 } project ? project : fileSystem.ProjectRoot.Name;
    }

    /// <summary>
    /// The file the project's workflow belongs in: named for the project, and — where another
    /// project of the same name got there first — for where this one is, so that ensuring one
    /// project's jobs can never overwrite another's.
    /// </summary>
    private async Task<IFile> Free(IDirectory root, string? directory, string name, CancellationToken ct)
    {
        var file = actions.File(root, Slug(name));
        if (!file.Exists)
        {
            return file;
        }

        var read = await actions.Read(file, ct);
        return read.IsSuccess && RunsTheTool(read.Value)
            ? actions.File(root, $"{Slug(name)}-{Slug(directory ?? root.Name)}")
            : file;
    }

    /// <summary>
    /// Whether the workflow runs this tool at all — for a file that isn't this project's, which
    /// makes it another project's.
    /// </summary>
    private bool RunsTheTool(ActionsWorkflow workflow) =>
        workflow.Jobs.Any(job => job.Invokes($"dotnet {tool.Command} "));

    /// <summary>
    /// A name as a file is spelled: lowercase, and separated the way paths and file names are.
    /// </summary>
    private static string Slug(string name) => name.ToLowerInvariant().Replace('.', '-').Replace('/', '-').Replace(' ', '-');

    private static string Named(IEnumerable<IJob> jobs) => string.Join(", ", jobs.Select(job => job.Name));
}
