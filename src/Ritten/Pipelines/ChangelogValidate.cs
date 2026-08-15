using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Validates the changelog against the release state.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's changelog options.</param>
/// <param name="release">The pipeline's release options.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("changelog", StepKind.Validation)]
public class ChangelogValidate(IPipelineLog log, IOptions<ChangelogOptions> options, IOptions<GitOptions> release, IBuildReport report, IChangelog changelogs)
{
    /// <summary>
    /// Validates the changelog for the given project and release state.
    /// </summary>
    /// <param name="project">The project being validated.</param>
    /// <param name="releaseState">The release state determined against the feed.</param>
    /// <param name="changelog">The parsed changelog (see <see cref="ReadChangelog"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public Task<StepResult> Run(Project project, ReleaseState releaseState, Changelog changelog, CancellationToken cancellationToken = default)
    {
        if (releaseState.Kind == ReleaseStateKind.Releasable)
        {
            // A prerelease ships whatever is in [Unreleased]; a release needs an entry of its own.
            // One or the other, never both — nothing writes a versioned heading before it ships.
            var entry = project.IsPrerelease ? changelog.Unreleased : changelog.Entry(project.Version);
            if (entry == null)
            {
                report.Section("Release").Failure(project.IsPrerelease
                    ? "Missing [Unreleased] changelog entry."
                    : $"Missing changelog entry for **{project.Version}**.");

                return Task.FromResult(StepResult.Failed(project.IsPrerelease
                    ? "No [Unreleased] entry found in changelog."
                    : $"No entry for version {project.Version} found in changelog."));
            }

            if (entry.IsEmpty)
            {
                report.Section("Release").Failure($"The changelog entry for **{project.Version}** is empty.");
                return Task.FromResult(StepResult.Failed($"Changelog entry for version {project.Version} is empty."));
            }
        }

        if (!string.IsNullOrEmpty(options.Value.RepositoryUrl))
        {
            var repository = new ChangelogRepository(options.Value.RepositoryUrl) { TagPrefix = release.Value.TagPrefix };
            var expected = changelogs.GenerateLinks(changelog, repository);
            if (!changelog.Links.SequenceEqual(expected))
            {
                var block = string.Join('\n', expected.Select(l => l.ToMarkdown()));
                report.Section("Release")
                    .Failure($"The version links in `{options.Value.File}` are missing or out of date. Replace the link block at the bottom of the file with:\n```\n{block}\n```");

                return Task.FromResult(StepResult.Failed(new Error(
                    $"The version links in {options.Value.File} are missing or out of date. " +
                    "Replace the link block at the bottom of the file with:")
                {
                    Verbatim = block
                }));
            }
        }

        if (releaseState.Kind == ReleaseStateKind.LatestInLine)
        {
            report.Section("Release").Success("New changes accrue under **[Unreleased]** until a release is prepared.");
            log.Detail("This version is the latest in its line; no changelog entry required.");
        }
        else
        {
            report.Section("Release").Success($"Changelog entry for **{project.Version}** is present.");
            log.Detail($"Found changelog entry for {project.Version}.");
        }

        return Task.FromResult(StepResult.Successful);
    }
}
