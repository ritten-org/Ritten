using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.GitHub;
using Ritten.Pipelines.DotNet;
using Ritten.Pipelines.Git;

namespace Ritten.Pipelines.GitHub;

/// <summary>
/// Creates the GitHub release for the version being shipped, with the changelog entry as its notes.
/// Prereleases are skipped, and so is a release a previous run already created.
/// Requires <see cref="Project"/> and <see cref="ChangelogEntry"/> in pipeline state (see <see cref="ExtractDotNetProject"/> and <see cref="ValidateChangelog"/>).
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's release options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="releases">The GitHub release service.</param>
/// <param name="changelogs">The changelog client.</param>
[DisplayName("Create GitHub Release")]
public class CreateGitHubRelease(
    ILogger<CreateGitHubRelease> logger,
    IOptions<GitOptions> options,
    IPipelineContext context,
    IReleaseService releases,
    IChangelog changelogs
) : IPipelineStep
{
    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var project = context.State.Get<Project>() ?? throw new Exception("Project info not found in state.");

        if (project.Version.IsPrerelease)
        {
            logger.LogInformation("Skipping GitHub Release for prerelease version {Version}; tag has still been pushed.", project.Version);
            return;
        }

        var tag = $"{options.Value.TagPrefix}{project.Version}";

        // A failed deploy may have already created the release; rerunning should carry on, not crash.
        if (await releases.Exists(tag, cancellationToken))
        {
            logger.LogInformation("GitHub Release {Tag} already exists; skipping.", tag);
            return;
        }

        var entry = context.State.Get<ChangelogEntry>() ?? throw new Exception("Changelog entry not found in state.");

        logger.LogInformation("Creating GitHub Release {Tag}.", tag);
        await releases.Create(tag, tag, changelogs.RenderEntry(entry), cancellationToken);
    }
}
