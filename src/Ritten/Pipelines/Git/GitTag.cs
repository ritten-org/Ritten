using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Pipelines.DotNet.Steps;

namespace Ritten.Pipelines.Git;

/// <summary>
/// Creates and pushes the release tag, skipping whatever a previous run already did so failed
/// deploys can be rerun. Requires <see cref="Project"/> in pipeline state
/// (see <see cref="ReadProject"/>).
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's release options.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="git">The git client.</param>
public class GitTag(IPipelineLog log, IOptions<GitOptions> options, IPipelineState state, IGit git) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "git tag";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (state.Get<Project>() is not { } project)
        {
            return StepResult.Failed("Project info not found in state.");
        }

        var tag = $"{options.Value.TagPrefix}{project.Version}";

        // A failed deploy may have already pushed the tag; rerunning should carry on, not crash.
        if (await git.RemoteTagExists("origin", tag, cancellationToken))
        {
            log.Skipped($"Tag {tag} already exists on origin; skipping.");
            return StepResult.Successful;
        }

        if (await git.TagExists(tag, cancellationToken))
        {
            log.Detail($"Tag {tag} already exists locally; pushing it.");
        }
        else
        {
            await git.CreateTag(tag, options.Value.CommitSha, cancellationToken);
        }

        await git.PushTag("origin", tag, cancellationToken);
        return StepResult.Successful;
    }
}
