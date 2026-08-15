using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Fails the pipeline when the changelog has no entry for the version being shipped.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's changelog options.</param>
/// <param name="release">The pipeline's release options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
public class ChangelogValidate(
    IPipelineLog log,
    IOptions<ChangelogOptions> options,
    IOptions<GitOptions> release,
    IFileSystem fileSystem,
    IPipelineState state,
    IBuildReport report,
    IChangelog changelogs
) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "changelog";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var project = state.Get<Project>();
        if (project == null)
        {
            return StepResult.Failed("Project info not found in state.");
        }

        if (state.Get<ReleaseState>() is not { } releaseState)
        {
            return StepResult.Failed("Release state not found in state.");
        }

        var changelogFile = fileSystem.ProjectRoot.GetFile(options.Value.File);
        if (!changelogFile.Exists)
        {
            report.Section("Release").Failure("The changelog file does not exist.");
            return StepResult.Failed($"Could not find changelog file '{options.Value.File}'.");
        }

        var changelog = await changelogs.Read(changelogFile, cancellationToken);

        ChangelogEntry? entry = null;
        if (releaseState.Kind == ReleaseStateKind.Releasable)
        {
            // A prerelease ships whatever is in [Unreleased]; a release needs an entry of its own.
            // One or the other, never both — nothing writes a versioned heading before it ships.
            entry = project.IsPrerelease ? changelog.Unreleased : changelog.Entry(project.Version);
            if (entry == null)
            {
                report.Section("Release").Failure(project.IsPrerelease
                    ? "Missing [Unreleased] changelog entry."
                    : $"Missing changelog entry for **{project.Version}**.");

                return StepResult.Failed(project.IsPrerelease
                    ? $"No [Unreleased] changelog entry found in {options.Value.File}."
                    : $"No changelog entry found for version {project.Version} in {options.Value.File}.");
            }

            if (entry.IsEmpty)
            {
                report.Section("Release").Failure($"The changelog entry for **{project.Version}** is empty.");
                return StepResult.Failed($"Changelog entry for version {project.Version} is empty.");
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

                return StepResult.Failed(new Error(
                    $"The version links in {options.Value.File} are missing or out of date. " +
                    "Replace the link block at the bottom of the file with:")
                {
                    Verbatim = block
                });
            }
        }

        if (releaseState.Kind == ReleaseStateKind.LatestInLine)
        {
            report.Section("Release").Success("New changes accrue under **[Unreleased]** until a release is prepared.");
            log.Detail("This version is the latest in its line; no changelog entry required.");
            return StepResult.Successful;
        }

        // CreateGitHubRelease reads this for the release notes.
        state.Set(entry);

        report.Section("Release").Success($"Changelog entry for **{project.Version}** is present.");
        log.Detail($"Found changelog entry for {project.Version}.");
        return StepResult.Successful;
    }
}
