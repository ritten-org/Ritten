using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Services;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Pack NuGet Package")]
public class Pack(IOptions<BuildOptions> options, ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        await commands.Run(
            command: "dotnet",
            arguments: [
                "pack", options.Value.ProjectFile,
                "--no-build",
                "--configuration", options.Value.Configuration,
                "--output", options.Value.ArtifactsDirectory
            ],
            cancellationToken
        );
    }
}
