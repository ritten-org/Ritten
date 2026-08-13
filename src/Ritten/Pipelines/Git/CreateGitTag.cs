using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Pipelines.DotNet;

namespace Ritten.Pipelines.Git;

/// <summary>
/// Creates and pushes the release tag, skipping whatever a previous run already did so failed
/// deploys can be rerun. Requires <see cref="Project"/> in pipeline state
/// (see <see cref="ExtractDotNetProject"/>).
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's release options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="git">The git client.</param>
[DisplayName("Create Git Tag")]
public class CreateGitTag(ILogger<CreateGitTag> logger, IOptions<GitOptions> options, IPipelineContext context, IGit git) : IPipelineStep
{
    /// <inheritdoc />
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
