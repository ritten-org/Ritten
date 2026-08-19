using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Reads the name and version of every package the repository ships.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's .NET options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("read projects", StepKind.Work)]
public class ReadProjects(IWorkflowLog log, IOptions<DotNetOptions> options, IFileSystem fileSystem, IDotNet dotnet)
{
    /// <summary>
    /// Reads every configured package project file.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<PackageSet>> Run(CancellationToken cancellationToken = default)
    {
        List<Project> packages = [];
        foreach (var path in options.Value.Projects)
        {
            var csproj = fileSystem.ProjectRoot.GetFile(path);
            if (!csproj.Exists)
            {
                return StepResult.Failed($"Could not find package project '{path}'.");
            }

            var project = await dotnet.ReadProject(csproj, cancellationToken);
            if (project.IsError)
            {
                return StepResult.Failed(project.Errors);
            }

            packages.Add(project.Value with { ProjectFile = path });
            log.Verbose($"Package {project.Value.Name} (v{project.Value.Version}) from {path}.");
        }

        log.Detail($"This repository ships: {string.Join(", ", packages.Select(p => p.Name))}.");
        return new PackageSet { Packages = packages };
    }
}
