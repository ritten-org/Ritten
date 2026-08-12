using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Publish NuGet Package")]
public class Publish(
    IOptions<BuildOptions> options,
    IOptions<NuGetOptions> nuget,
    IPipelineContext context,
    ICommandRunner commands,
    IBuildReport report
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nuget.Value.ApiKey))
        {
            throw new Exception("The NuGet API key is not configured; set NuGet__ApiKey for the deploy pipeline.");
        }

        var packageFile = context.FileSystem.CurrentDirectory
            .GetDirectory(options.Value.ArtifactsDirectory)
            .GetFiles("*.nupkg")
            .Single();

        var dotnetPublish = Command
            .Create("dotnet")
            .WithArguments("nuget", "push", packageFile.AbsolutePath)
            .AndArguments("--source", nuget.Value.Feed)
            .AndArguments("--api-key", nuget.Value.ApiKey)
            .AndArguments("--skip-duplicate")
            .RedactArguments()
            .ThrowOnError();
        await commands.Run(dotnetPublish, cancellationToken);

        if (context.State.Get<Project>() is { } project)
        {
            report.Section("Release").Success($"Published **{project.Name} {project.Version}** to NuGet.");
        }
    }
}
