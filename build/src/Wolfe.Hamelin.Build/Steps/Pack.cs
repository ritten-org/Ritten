using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Pack NuGet Package")]
public class Pack(IOptions<BuildOptions> options, ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var dotnetPack = Command
            .Run("dotnet")
            .WithArguments("pack", options.Value.ProjectFile, "--no-build")
            .AndArguments("--configuration", options.Value.Configuration)
            .AndArguments("--output", options.Value.ArtifactsDirectory);

        await commands.Run(dotnetPack, cancellationToken);
    }
}
