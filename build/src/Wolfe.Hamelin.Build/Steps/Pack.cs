using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Pack NuGet Package")]
public class Pack(IOptions<BuildOptions> options, IPipelineContext context, IDotNet dotnet) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Pack(
            new PackArgs
            {
                Project = options.Value.ProjectFile,
                Configuration = options.Value.Configuration,
                NoBuild = true,
                Output = context.FileSystem.CurrentDirectory.GetDirectory(options.Value.ArtifactsDirectory)
            },
            cancellationToken);

        context.State.Set(result);
    }
}
