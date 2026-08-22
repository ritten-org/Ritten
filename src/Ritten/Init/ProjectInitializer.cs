using System.Reflection;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Init;

/// <summary>
/// Sets a repository up to run a workflow, or reports how far its scaffolding has drifted.
/// </summary>
/// <param name="workflows">The workflows this tool can scaffold for.</param>
/// <param name="log">The workflow log.</param>
/// <param name="prompt">The prompt used to confirm a derived workflow.</param>
/// <param name="projectFile">The name the host gives the project file.</param>
public sealed class ProjectInitializer(
    WorkflowRegistry workflows,
    IWorkflowLog log,
    IWorkflowPrompt prompt,
    string projectFile = "ritten.json"
)
{
    /// <summary>
    /// Scaffolds the repository in the given directory.
    /// </summary>
    /// <param name="directory">The repository to set up.</param>
    /// <param name="name">The workflow to scaffold for, or null to derive one.</param>
    /// <param name="check">Report what's missing or drifted without writing anything.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<ExitCode> Run(string directory, string? name, bool check, CancellationToken ct = default)
    {
        var root = new PhysicalDirectory(directory);
        var workflow = await SelectWorkflow(root, name, ct);
        if (workflow.IsError)
        {
            foreach (var error in workflow.Errors)
            {
                log.Error(error.Message);
            }

            return ExitCode.ConfigurationError;
        }

        var files = RepositoryScaffold.For(workflow.Value, ShippedProject(root), Version, projectFile);
        var outcomes = await new Scaffolder(FileSystemAt(root)).Apply(files, root, check, ct);

        return Report(workflow.Value, outcomes, check);
    }

    /// <summary>
    /// The version doing the scaffolding is the one the repository gets pinned to.
    /// </summary>
    private static string Version =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";

    private async Task<Result<IWorkflow>> SelectWorkflow(IDirectory root, string? name, CancellationToken ct)
    {
        var known = $"Known workflows: {string.Join(", ", workflows.Workflows.Select(w => w.Name))}.";
        if (name is { Length: > 0 })
        {
            return workflows.Find(name) is { } named
                ? new Result<IWorkflow>(named)
                : new Result<IWorkflow>([Result.Error($"There is no workflow named '{name}'."), Result.Error(known)]);
        }

        // Nothing named, so propose from what's in the repository.
        if (SuggestWorkflow(root) is not { } proposal)
        {
            return new Result<IWorkflow>([Result.Error("Name the workflow to scaffold for."), Result.Error(known)]);
        }

        if (!prompt.IsInteractive)
        {
            return new Result<IWorkflow>([
                Result.Error($"This looks like a '{proposal.Name}' repository, but nobody is here to confirm it."),
                Result.Error($"Name the workflow to scaffold for. {known}")
            ]);
        }

        return await prompt.Confirm($"This looks like a '{proposal.Name}' repository. Scaffold for that?", ct)
            ? new Result<IWorkflow>(proposal)
            : new Result<IWorkflow>([Result.Error("Nothing scaffolded."), Result.Error(known)]);
    }

    /// <summary>
    /// Reads the repository the way a person would: a project that packs as a tool means a tool,
    /// one that packs at all means a package, and anything else just builds.
    /// </summary>
    private IWorkflow? SuggestWorkflow(IDirectory root)
    {
        var projects = Projects(root).ToList();
        var name = projects.Any(p => Declares(p, "<PackAsTool>true</PackAsTool>"))
            ? "dotnet-tool"
            : projects.Any(p => Declares(p, "<PackageId>") || Declares(p, "<IsPackable>true</IsPackable>"))
                ? "dotnet-package"
                : "dotnet";

        return workflows.Find(name);
    }

    /// <summary>
    /// The project the repository ships, when exactly one candidate stands out.
    /// </summary>
    private static string? ShippedProject(IDirectory root)
    {
        var projects = Projects(root).Where(p => !IsTests(p)).ToList();
        return projects.Count == 1 ? Relative(root, projects[0]) : null;
    }

    private static IEnumerable<IFile> Projects(IDirectory root) =>
        root.GetFiles("**/*.csproj").Where(file => !file.AbsolutePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static bool IsTests(IFile project) =>
        project.NameWithoutExtension.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
        || project.AbsolutePath.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static bool Declares(IFile project, string element)
    {
        using var reader = new StreamReader(project.OpenRead());
        return reader.ReadToEnd().Contains(element, StringComparison.OrdinalIgnoreCase);
    }

    private static string Relative(IDirectory root, IFile file) =>
        Path.GetRelativePath(root.AbsolutePath, file.AbsolutePath).Replace(Path.DirectorySeparatorChar, '/');

    private ExitCode Report(IWorkflow workflow, IReadOnlyList<(ScaffoldedFile File, ScaffoldOutcome Outcome)> outcomes, bool check)
    {
        foreach (var (file, outcome) in outcomes)
        {
            switch (outcome)
            {
                case ScaffoldOutcome.Written when check:
                    log.Warning($"{file.Path} is missing.");
                    break;
                case ScaffoldOutcome.Written:
                    log.Detail($"Wrote {file.Path}.");
                    break;
                case ScaffoldOutcome.Matches:
                    log.Verbose($"{file.Path} is up to date.");
                    break;
                case ScaffoldOutcome.Differs when check:
                    log.Warning($"{file.Path} differs from what the {workflow.Label} workflow expects.");
                    break;
                default:
                    log.Skipped($"{file.Path} already exists; left as it is.");
                    break;
            }
        }

        if (!check)
        {
            log.Status($"Set up for the {workflow.Label} workflow. Run `dotnet tool restore`, then `dotnet ritten check`.");
            return ExitCode.Success;
        }

        var drifted = outcomes.Count(o => o.Outcome != ScaffoldOutcome.Matches);
        if (drifted == 0)
        {
            log.Status($"The scaffolding matches the {workflow.Label} workflow.");
            return ExitCode.Success;
        }

        log.Error($"{drifted} of {outcomes.Count} files are missing or out of date. Run `ritten init` to see what's expected.");
        return ExitCode.Failed;
    }

    private static IFileSystem FileSystemAt(IDirectory root) => new ScaffoldFileSystem(root);

    private sealed class ScaffoldFileSystem(IDirectory root) : IFileSystem
    {
        public IDirectory ProjectRoot => root;
        public IDirectory Artifacts => root.GetDirectory("artifacts");
        public IDirectory Temp => root.GetDirectory("temp");
    }
}
