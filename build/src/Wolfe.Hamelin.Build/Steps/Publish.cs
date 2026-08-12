using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Reporting;
using Wolfe.Hamelin.Commands;

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

        var dotnetPublish = Command
            .Run("dotnet")
            .WithArguments("nuget", "push", packageFile.AbsolutePath)
            .AndArguments("--source", options.Value.NuGetFeed)
            .AndArguments("--api-key", options.Value.NuGetApiKey)
            .AndArguments("--skip-duplicate")
            .Sensitive();
        await commands.Run(dotnetPublish, cancellationToken);

        if (context.State.Get<ProjectInfo>() is { } projectInfo)
        {
            report.Section("Release").Success($"Published **{projectInfo.Name} {projectInfo.Version}** to NuGet.");
        }
    }
}
