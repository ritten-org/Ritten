using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Create Git Tag")]
public class CreateTag(ILogger<CreateTag> logger, IOptions<BuildOptions> options, IPipelineContext context, ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var project = context.State.Get<Project>() ?? throw new Exception("Project info not found in state.");
        var tag = $"v{project.Version}";

        // A failed deploy may have already pushed the tag; rerunning should carry on, not crash.
        var remoteTag = await commands.Run(
            Command.Create("git").WithArguments("ls-remote", "--tags", "origin", $"refs/tags/{tag}").ThrowOnError(),
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(remoteTag.StandardOutput))
        {
            logger.LogInformation("Tag {Tag} already exists on origin; skipping.", tag);
            return;
        }

        var localTag = await commands.Run(
            Command.Create("git").WithArguments("rev-parse", "--verify", "--quiet", $"refs/tags/{tag}"),
            cancellationToken);
        if (localTag.IsError)
        {
            var gitTag = Command.Create("git").WithArguments("tag", tag).ThrowOnError();
            if (!string.IsNullOrEmpty(options.Value.CommitSha))
            {
                gitTag = gitTag.AndArguments(options.Value.CommitSha);
            }

            logger.LogInformation("Creating git tag {Tag}.", tag);
            await commands.Run(gitTag, cancellationToken);
        }
        else
        {
            logger.LogInformation("Tag {Tag} already exists locally; pushing it.", tag);
        }

        var gitPush = Command.Create("git").WithArguments("push", "origin", tag).ThrowOnError();
        await commands.Run(gitPush, cancellationToken);
    }
}
