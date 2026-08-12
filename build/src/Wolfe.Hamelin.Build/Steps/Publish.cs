using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Reporting;
using Wolfe.Hamelin.Build.Services;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Publish NuGet Package")]
public class Publish(
    IOptions<BuildOptions> options,
    IPipelineContext context,
    ICommandRunner commands,
    IBuildReport report
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var packageFile = context.FileSystem.CurrentDirectory
            .GetDirectory(options.Value.ArtifactsDirectory)
            .GetFiles("*.nupkg")
            .Single();

        await commands.Run(
            command: "dotnet",
            arguments: [
                "nuget", "push",
                packageFile.AbsolutePath,
                "--source", options.Value.NuGetFeed,
                "--api-key", options.Value.NuGetApiKey,
                "--skip-duplicate"
            ],
            cancellationToken
        );

        if (context.State.Get<ProjectInfo>() is { } projectInfo)
        {
            report.Section("Release").Success($"Published **{projectInfo.Name} {projectInfo.Version}** to NuGet.");
        }
    }
}
