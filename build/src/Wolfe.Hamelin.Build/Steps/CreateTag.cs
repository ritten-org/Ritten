using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Git;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Create Git Tag")]
public class CreateTag(ILogger<CreateTag> logger, IOptions<ReleaseOptions> options, IPipelineContext context, IGit git) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var project = context.State.Get<Project>() ?? throw new Exception("Project info not found in state.");
        var tag = $"{options.Value.TagPrefix}{project.Version}";

        // A failed deploy may have already pushed the tag; rerunning should carry on, not crash.
        if (await git.RemoteTagExists("origin", tag, cancellationToken))
        {
            logger.LogInformation("Tag {Tag} already exists on origin; skipping.", tag);
            return;
        }

        if (await git.TagExists(tag, cancellationToken))
        {
            logger.LogInformation("Tag {Tag} already exists locally; pushing it.", tag);
        }
        else
        {
            logger.LogInformation("Creating git tag {Tag}.", tag);
            await git.CreateTag(tag, options.Value.CommitSha, cancellationToken);
        }

        await git.PushTag("origin", tag, cancellationToken);
    }
}
