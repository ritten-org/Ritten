using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Reads the package name and version from the configured project file.
/// Sets <see cref="Project"/> in pipeline state for later steps.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="dotnet">The dotnet client.</param>
public class ExtractDotNetProject(IPipelineLog log, IOptions<DotNetOptions> options, IFileSystem fileSystem, IPipelineState state, IDotNet dotnet) : IPipelineStep
{
    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var csproj = fileSystem.CurrentDirectory.GetFile(options.Value.ProjectFile);
        if (!csproj.Exists)
        {
            return StepResult.Failed($"Could not find project file '{options.Value.ProjectFile}'.");
        }

        var project = await dotnet.ReadProject(csproj, cancellationToken);
        if (project is null)
        {
            return StepResult.Failed($"The project '{options.Value.ProjectFile}' could not be read.");
        }

        state.Set(project);

        log.Status($"Extracted project info: {project.Name} (v{project.Version})");
        return StepResult.Successful;
    }
}
