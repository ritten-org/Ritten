using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Releases;

namespace Ritten.GitHub.Steps;

/// <summary>
/// Creates the GitHub release for the version being shipped, with its changelog entry as the
/// notes. Prereleases are skipped, and so is a release a previous run already created.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's release options.</param>
/// <param name="releases">The GitHub release service.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("gh release create", StepKind.Publish)]
public class GitHubRelease(
    IWorkflowLog log,
    IOptions<GitOptions> options,
    IReleaseService releases,
    IChangelog changelogs
)
{
    /// <summary>
    /// Creates the GitHub release.
    /// </summary>
    /// <param name="project">The project being released.</param>
    /// <param name="changelog">The validated changelog the release notes come from.</param>
    /// <param name="releaseState">The release state, used to keep a backport from being marked latest.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(Project project, Changelog changelog, ReleaseState releaseState, CancellationToken cancellationToken = default)
    {
        if (project.IsPrerelease)
        {
            log.Skipped($"Skipping GitHub Release for prerelease version {project.Version}; tag has still been pushed.");
            return StepResult.Successful;
        }

        if (RepositoryPath.Parse(project.Repository) is not { } repository)
        {
            return StepResult.Failed("The GitHub repository can't be determined. Set `repository` in ritten.json, or `RepositoryUrl` in the project file.");
        }

        var tag = $"{options.Value.TagPrefix}{project.Version}";

        // A failed deploy may have already created the release; rerunning should carry on, not crash.
        if (await releases.Exists(repository, tag, cancellationToken))
        {
            log.Skipped($"GitHub Release {tag} already exists; skipping.");
            return StepResult.Successful;
        }

        if (changelog.Entry(project.Version) is not { } entry)
        {
            return StepResult.Failed($"No changelog entry found for version {project.Version}.");
        }

        // A backport must not displace the repository's real latest release.
        var makeLatest = releaseState.LatestVersion is null || project.Version > releaseState.LatestVersion;

        await releases.Create(repository, tag, tag, changelogs.RenderEntry(entry), makeLatest, cancellationToken);
        return StepResult.Successful;
    }
}
