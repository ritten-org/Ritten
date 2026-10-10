using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Git.Steps;

/// <summary>
/// Creates and pushes the release tag, skipping whatever a previous run already did so failed deploys can be rerun.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The commit to tag, from the environment.</param>
/// <param name="git">The git client.</param>
/// <param name="fileSystem">The file system, whose project root is the repository tagged.</param>
[Step("git tag", StepKind.Publish)]
public class GitTag(IWorkflowLog log, IOptions<GitOptions> options, IGit git, IFileSystem fileSystem)
{
    /// <summary>
    /// Tags the release and pushes the tag.
    /// </summary>
    /// <param name="release">How the project releases, including how its tags are named.</param>
    /// <param name="project">The project being released (see <see cref="ResolveRelease"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(ReleaseSettings release, Project project, CancellationToken cancellationToken = default)
    {
        var tag = $"{release.TagPrefix}{project.Version}";

        // A failed deployment may have already pushed the tag; rerunning should carry on, not crash.
        if (await git.RemoteTagExists(fileSystem.ProjectRoot, "origin", tag, cancellationToken))
        {
            log.Skipped($"Tag {tag} already exists on origin; skipping.");
            return StepResult.Successful;
        }

        if (await git.TagExists(fileSystem.ProjectRoot, tag, cancellationToken))
        {
            log.Detail($"Tag {tag} already exists locally; pushing it.");
        }
        else
        {
            await git.CreateTag(fileSystem.ProjectRoot, tag, options.Value.CommitSha, cancellationToken);
        }

        await git.PushTag(fileSystem.ProjectRoot, "origin", tag, cancellationToken);
        return StepResult.Successful;
    }
}
