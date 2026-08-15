using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Reads the package name and version from the configured project file.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="dotnet">The dotnet client.</param>
[Step("read project", StepKind.Work)]
public class ReadProject(IPipelineLog log, IOptions<DotNetOptions> options, IFileSystem fileSystem, IDotNet dotnet)
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

        log.Detail($"Extracted project info: {project.Value.Name} (v{project.Value.Version})");
        return project.Value;
    }
}
