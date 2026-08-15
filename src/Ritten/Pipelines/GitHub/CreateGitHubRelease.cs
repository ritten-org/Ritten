using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Pipelines.Git;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Pipelines.GitHub;

/// <summary>
/// Creates the GitHub release for the version being shipped, with the changelog entry as its notes.
/// Prereleases are skipped, and so is a release a previous run already created.
/// Requires <see cref="Project"/> and <see cref="ChangelogEntry"/> in pipeline state (see <see cref="ExtractDotNetProject"/> and <see cref="ValidateChangelog"/>).
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's release options.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="releases">The GitHub release service.</param>
/// <param name="changelogs">The changelog client.</param>
public class CreateGitHubRelease(
    IPipelineLog log,
    IOptions<GitOptions> options,
    IPipelineState state,
    IReleaseService releases,
    IChangelog changelogs
) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "gh release create";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (state.Get<Project>() is not { } project)
        {
            return StepResult.Failed("Project info not found in state.");
        }

        if (project.IsPrerelease)
        {
            log.Detail($"Skipping GitHub Release for prerelease version {project.Version}; tag has still been pushed.");
            return StepResult.Successful;
        }

        var tag = $"{options.Value.TagPrefix}{project.Version}";

        // A failed deploy may have already created the release; rerunning should carry on, not crash.
        if (await releases.Exists(tag, cancellationToken))
        {
            log.Detail($"GitHub Release {tag} already exists; skipping.");
            return StepResult.Successful;
        }

        if (state.Get<ChangelogEntry>() is not { } entry)
        {
            return StepResult.Failed("Changelog entry not found in state.");
        }

        log.Detail($"Creating GitHub Release {tag}.");
        await releases.Create(tag, tag, changelogs.RenderEntry(entry), cancellationToken);
        return StepResult.Successful;
    }
}
