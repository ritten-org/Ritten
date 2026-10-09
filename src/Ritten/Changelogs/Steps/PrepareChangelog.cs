using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Gives the unreleased notes their version heading and brings the version links up to date.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="changelogs">The changelog client.</param>
/// <param name="time">The clock the release is dated by.</param>
[Step("prepare changelog", StepKind.Work)]
public class PrepareChangelog(
    IWorkflowLog log,
    IFileSystem fileSystem,
    IChangelog changelogs,
    TimeProvider time
)
{
    /// <summary>
    /// Rolls the unreleased notes into the prepared version and regenerates the links.
    /// </summary>
    /// <param name="changelogFile">The changelog the project keeps.</param>
    /// <param name="releaseSettings">How the project releases, including how its tags are named.</param>
    /// <param name="changelog">The changelog as it stands (see <see cref="ReadChangelog"/>).</param>
    /// <param name="project">The project being released, for the repository the links point at.</param>
    /// <param name="release">The version being prepared (see <see cref="DecideVersion"/>).</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(ChangelogSettings changelogFile, ReleaseSettings releaseSettings, Changelog changelog, Project project, PreparedRelease release, CancellationToken ct = default)
    {
        var hasChangelog = changelog.Entry(release.Version) is not null;
        var rolled = WithRelease(changelogFile, changelog, release, out var entry);
        var linked = WithLinks(releaseSettings, rolled, project);

        // Rendering both is the only honest comparison: the parser is tolerant of formatting the
        // renderer normalizes, so a byte-identical render is what "nothing to do" really means.
        var before = changelogs.Render(changelog);
        var after = changelogs.Render(linked);
        if (before == after)
        {
            log.Skipped($"{changelogFile.File} needs no changes.");
            return StepResult.Successful;
        }

        await changelogs.Write(fileSystem.ProjectRoot.GetFile(changelogFile.File), linked, ct);

        log.Detail(entry switch
        {
            null => $"Updated the version links in {changelogFile.File}.",
            _ when hasChangelog => $"Added the unreleased notes to the entry {release.Version} already had in {changelogFile.File}.",
            _ => $"Rolled the unreleased notes into {release.Version} in {changelogFile.File}."
        });

        return StepResult.Successful;
    }

    /// <summary>
    /// Dates the unreleased entry and gives it its version — joining the entry that version already
    /// has, when it has one — and leaves every other entry alone. The body renders verbatim, so
    /// nobody's prose is reformatted on the way through.
    /// </summary>
    private Changelog WithRelease(ChangelogSettings changelogFile, Changelog changelog, PreparedRelease release, out ChangelogEntry? rolled)
    {
        rolled = null;

        // Nothing to roll is worth saying rather than passing over: the version still moves, and
        // the changelog check will then refuse a release that describes nothing. Writing that
        // description is the one thing here nobody but the author can do.
        if (changelog.Unreleased is not { } unreleased || unreleased.IsEmpty)
        {
            log.Warning(
                $"{changelogFile.File} has no unreleased notes, so {release.Version} has nothing to describe it. " +
                "Add them under an [Unreleased] heading."
            );
            return changelog;
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var entries = changelog.Entries.ToList();

        // The version may already have an entry: prepared once before and not yet shipped.
        // Its notes join the ones already under that heading.
        if (changelog.Entry(release.Version) is { } existing)
        {
            rolled = existing.Merge(unreleased) with { Date = today };
            entries[entries.IndexOf(existing)] = rolled;
            entries.Remove(unreleased);
            return changelog with { Entries = entries };
        }

        rolled = unreleased with { Version = release.Version, Date = today };
        entries[entries.IndexOf(unreleased)] = rolled;
        return changelog with { Entries = entries };
    }

    private Changelog WithLinks(ReleaseSettings releaseSettings, Changelog changelog, Project project)
    {
        if (project.Repository is not { Length: > 0 } repository)
        {
            log.Warning("The repository URL is unknown, so the version links are left as they are.");
            return changelog;
        }

        var links = changelogs.GenerateLinks(changelog, new ChangelogRepository(repository) { TagPrefix = releaseSettings.TagPrefix });
        return changelog with { Links = links };
    }
}
