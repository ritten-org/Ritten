using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Decides which version the next release will be for.
/// </summary>
/// <param name="job">The job being run.</param>
/// <param name="requested">The version the caller named, when they named one.</param>
/// <param name="log">The workflow log.</param>
/// <param name="prompt">The prompt used to confirm a derived version.</param>
[Step("decide version", StepKind.Work)]
public class DecideVersion(WorkflowJob job, RequestedVersion requested, IWorkflowLog log, IWorkflowPrompt prompt)
{
    /// <summary>
    /// Determines the version to prepare.
    /// </summary>
    /// <param name="project">The project being released (see <see cref="DotNet.Steps.ResolveRelease"/>).</param>
    /// <param name="changelog">The changelog (see <see cref="ReadChangelog"/>).</param>
    /// <param name="release">The release state determined against the feed.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<PreparedRelease>> Run(Project project, Changelog changelog, ReleaseState release, CancellationToken ct = default)
    {
        // A version the caller names is taken as given: they know something the changelog doesn't.
        if (requested.Version is { } named)
        {
            var bumped = named != project.Version;
            log.Detail(bumped
                ? $"Preparing {named}, as asked (currently {project.Version})."
                : $"Preparing {named}, as asked — the version the project already declares.");
            return new PreparedRelease(named, bumped, $"named with --{ReleaseArguments.Version.Name}");
        }

        // An unpublished version is already the next one: the project was bumped and never shipped,
        // so preparing again would skip a version nobody released.
        if (!release.Published)
        {
            log.Detail($"Preparing {project.Version}, which the project already declares and hasn't published.");
            return new PreparedRelease(project.Version, false, "already declared, not yet published");
        }

        // Read once, so the version proposed and the reason given for it cannot disagree.
        var kind = changelog.Unreleased?.ReleaseKind ?? ReleaseKind.None;
        var nextVersion = project.Version.Next(kind);
        var because = Because(kind);
        if (job.AutoApprove)
        {
            log.Skipped($"Preparing {nextVersion} ({because}), approved automatically by --{WorkflowArguments.AutoApprove}.");
            return new PreparedRelease(nextVersion, true, because);
        }

        if (!prompt.IsInteractive)
        {
            // The same bargain the approval gate strikes: never guess a release number unwatched.
            return StepResult.Failed(
                $"{project.Version} is published, so {job.Name} would move to {nextVersion} ({because}). " +
                $"Pass --{ReleaseArguments.Version.Name} to name the version, or --{WorkflowArguments.AutoApprove} to take the derived one.");
        }

        if (!await prompt.Confirm($"Prepare {nextVersion}? ({because}, currently {project.Version})", ct))
        {
            return StepResult.Failed($"Nothing prepared. Pass --{ReleaseArguments.Version.Name} to name the version yourself.");
        }

        return new PreparedRelease(nextVersion, true, because);
    }

    private static string Because(ReleaseKind kind) => kind switch
    {
        ReleaseKind.Breaking => "the unreleased notes change or remove what shipped",
        ReleaseKind.Features => "the unreleased notes add to what shipped",
        ReleaseKind.Fixes => "the unreleased notes only fix what shipped",
        _ => "nothing unreleased to describe it"
    };
}
