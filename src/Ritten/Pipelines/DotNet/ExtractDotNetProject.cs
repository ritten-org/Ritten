using System.ComponentModel;
using Ritten.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.DotNet;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// Reads the package name and version from the configured project file.
/// Sets <see cref="Project"/> in pipeline state for later steps.
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's build options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="dotnet">The dotnet client.</param>
[DisplayName("Extract .NET Project")]
public class ExtractDotNetProject(ILogger<ExtractDotNetProject> logger, IOptions<DotNetOptions> options, IPipelineContext context, IDotNet dotnet) : IPipelineStep
{
    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var csproj = context.FileSystem.CurrentDirectory.GetFile(options.Value.ProjectFile);
        if (!csproj.Exists)
        {
            throw new FileNotFoundException("Could not find project file", csproj.AbsolutePath);
        }

        var project = await dotnet.ReadProject(csproj, cancellationToken);
        context.State.Set(project);

        logger.LogInformation("Extracted project info: {ProjectName} (v{Version})", project.Name, project.Version);
    }
}
