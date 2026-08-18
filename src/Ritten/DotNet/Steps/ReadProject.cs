using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Git;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Reads the package name and version from the configured project file.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's build options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="git">The git client.</param>
[Step("read project", StepKind.Work)]
public class ReadProject(IWorkflowLog log, IOptions<DotNetOptions> options, IFileSystem fileSystem, IDotNet dotnet, IGit git)
{
    /// <summary>
    /// Reads the configured project file.
    /// </summary>
    public async Task<StepResult<Project>> Run(CancellationToken cancellationToken = default)
    {
        var csproj = fileSystem.ProjectRoot.GetFile(options.Value.ProjectFile);
        if (!csproj.Exists)
        {
            return StepResult.Failed($"Could not find project file '{options.Value.ProjectFile}'.");
        }

        var project = await dotnet.ReadProject(csproj, cancellationToken);
        if (project.IsError)
        {
            return StepResult.Failed(project.Errors);
        }

        // Resolved once, here, so no consumer coalesces sources again: the explicit setting
        // wins, then the project file's RepositoryUrl, then the origin remote.
        var repository = options.Value.Repository
            ?? project.Value.Repository
            ?? RepositoryUrls.ToWebUrl(await git.GetRemoteUrl("origin", cancellationToken));

        log.Detail($"Extracted project info: {project.Value.Name} (v{project.Value.Version})");
        log.Verbose($"Repository: {repository ?? "unknown"}.");
        return project.Value with { Repository = repository };
    }
}
