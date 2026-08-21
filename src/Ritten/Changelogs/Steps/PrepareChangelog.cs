using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Gives the unreleased notes their version heading and brings the version links up to date.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's changelog options.</param>
/// <param name="gitOptions">The workflow's release options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="changelogs">The changelog client.</param>
/// <param name="time">The clock the release is dated by.</param>
[Step("prepare changelog", StepKind.Work)]
public class PrepareChangelog(
    IWorkflowLog log,
    IOptions<ChangelogOptions> options,
    IOptions<GitOptions> gitOptions,
    IFileSystem fileSystem,
    IChangelog changelogs,
    TimeProvider time
)
{
    /// <summary>
    /// Rolls the unreleased notes into the prepared version and regenerates the links.
    /// </summary>
    /// <param name="changelog">The changelog as it stands (see <see cref="ReadChangelog"/>).</param>
    /// <param name="project">The project being released, for the repository the links point at.</param>
    /// <param name="release">The version being prepared (see <see cref="DecideVersion"/>).</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(Changelog changelog, Project project, PreparedRelease release, CancellationToken ct = default)
    {
        var rolled = WithRelease(changelog, release, out var entry);
        var linked = WithLinks(rolled, project);

        // Rendering both is the only honest comparison: the parser is tolerant of formatting the
        // renderer normalizes, so a byte-identical render is what "nothing to do" really means.
        var before = changelogs.Render(changelog);
        var after = changelogs.Render(linked);
        if (before == after)
        {
            log.Skipped($"{options.Value.File} needs no changes.");
            return StepResult.Successful;
        }

        await changelogs.Write(fileSystem.ProjectRoot.GetFile(options.Value.File), linked, ct);

        log.Detail(entry switch
        {
            null => $"Updated the version links in {options.Value.File}.",
            _ => $"Rolled the unreleased notes into {release.Version} in {options.Value.File}."
        });

        return StepResult.Successful;
    }

    /// <summary>
    /// Dates the unreleased entry and gives it its version, leaving every other entry alone.
    /// The body renders verbatim, so nobody's prose is reformatted on the way through.
    /// </summary>
    private Changelog WithRelease(Changelog changelog, PreparedRelease release, out ChangelogEntry? rolled)
    {
        rolled = null;

        // Nothing to roll is worth saying rather than passing over: the version still moves, and
        // the changelog check will then refuse a release that describes nothing. Writing that
        // description is the one thing here nobody but the author can do.
        if (changelog.Unreleased is not { } unreleased || unreleased.IsEmpty)
        {
            log.Warning(
                $"{options.Value.File} has no unreleased notes, so {release.Version} has nothing to describe it. " +
                "Add them under an [Unreleased] heading."
            );
            return changelog;
        }

        rolled = unreleased with
        {
            Version = release.Version,
            Date = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)
        };

        var entries = changelog.Entries.ToList();
        entries[entries.IndexOf(unreleased)] = rolled;
        return changelog with { Entries = entries };
    }

    private Changelog WithLinks(Changelog changelog, Project project)
    {
        if (project.Repository is not { Length: > 0 } repository)
        {
            log.Warning("The repository URL is unknown, so the version links are left as they are.");
            return changelog;
        }

        var links = changelogs.GenerateLinks(changelog, new ChangelogRepository(repository) { TagPrefix = gitOptions.Value.TagPrefix });
        return changelog with { Links = links };
    }
}
