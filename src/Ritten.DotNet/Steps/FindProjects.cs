using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Finds what the repository builds, for the jobs that run before anything says.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
[Step("find projects", StepKind.Work)]
public class FindProjects(IWorkflowLog log, IFileSystem fileSystem)
{
    /// <summary>
    /// Reads the repository's projects off the disk.
    /// </summary>
    public StepResult<DiscoveredProjects> Run()
    {
        var root = fileSystem.ProjectRoot;
        var projects = DotNetProjects.Projects(root).ToList();
        var found = new DiscoveredProjects(
            [.. projects.Where(p => !DotNetProjects.IsTests(p)).Select(root.RelativePath)],
            [.. projects.Where(DotNetProjects.IsTests).Select(root.RelativePath)]
        );

        log.Detail(found switch
        {
            { Shipped.Count: 0, Tests.Count: 0 } => "No projects here yet.",
            { Tests.Count: 0 } => $"Found {found.Shipped.Count} project(s).",
            _ => $"Found {found.Shipped.Count} project(s) and {found.Tests.Count} test project(s)."
        });

        foreach (var project in found.Shipped)
        {
            log.Verbose(project);
        }

        return found;
    }
}
