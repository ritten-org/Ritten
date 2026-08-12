using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Extract Project Information")]
public class ExtractProject(ILogger<ExtractProject> logger, IOptions<BuildOptions> options, IPipelineContext context, IDotNet dotnet) : IPipelineStep
{
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
