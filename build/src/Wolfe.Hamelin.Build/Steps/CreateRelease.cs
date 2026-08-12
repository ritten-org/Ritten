using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Create GitHub Release")]
public class CreateRelease(
    ILogger<CreateRelease> logger,
    IOptions<BuildOptions> options,
    IPipelineContext context,
    ICommandRunner commands,
    IChangelog changelogs
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var projectInfo = context.State.Get<Project>() ?? throw new Exception("Project info not found in state.");

        if (projectInfo.Version.IsPrerelease)
        {
            logger.LogInformation("Skipping GitHub Release for prerelease version {Version}; tag has still been pushed.", projectInfo.Version);
            return;
        }

        var entry = context.State.Get<ChangelogEntry>() ?? throw new Exception("Changelog entry not found in state.");

        var tag = $"v{projectInfo.Version}";
        var notesFile = context.FileSystem.CurrentDirectory
            .GetDirectory(options.Value.TempDirectory)
            .GetFile($"release-notes-{projectInfo.Version}.md");

        Directory.CreateDirectory(Path.GetDirectoryName(notesFile.AbsolutePath)!);
        await File.WriteAllTextAsync(notesFile.AbsolutePath, changelogs.RenderEntry(entry), cancellationToken);

        logger.LogInformation("Creating GitHub Release {Tag}.", tag);

        var ghRelease = Command
            .Create("gh")
            .WithArguments("release", "create", tag)
            .AndArguments("--title", tag)
            .AndArguments("--notes-file", notesFile.AbsolutePath)
            .ThrowOnError();
        await commands.Run(ghRelease, cancellationToken);
    }
}
