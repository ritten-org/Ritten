using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
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
public class ValidateChangelog(
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
    public string Name => "Validate changelog";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.Skip)
        {
            log.Detail("Skipping changelog check.");
            return StepResult.Successful;
        }

        var project = state.Get<Project>();
        if (project == null)
        {
            return StepResult.Failed("Project info not found in state.");
        }

        var changelogFile = fileSystem.ProjectRoot.GetFile(options.Value.File);
        if (!changelogFile.Exists)
        {
            report.Section("Release").Failure("The changelog file does not exist.");
            return StepResult.Failed($"Could not find changelog file '{options.Value.File}'.");
        }

        var changelog = await changelogs.Read(changelogFile, cancellationToken);

        var isPrerelease = project.Version.IsPrerelease || project.Version < NuGetVersion.Parse("1.0.0");
        if (isPrerelease)
        {
            if (changelog.Unreleased == null)
            {
                report.Section("Release").Failure("Missing [Unreleased] changelog entry for pre-release version.");
                return StepResult.Failed($"No changelog entry found for prerelease version {project.Version} in {options.Value.File}.");
            }
            state.Set(changelog.Unreleased);
        }

        var entry = changelog.Entry(project.Version);
        if (entry == null)
        {
            report.Section("Release").Failure($"Missing changelog entry for **{project.Version}**.");
            return StepResult.Failed($"No changelog entry found for version {project.Version} in {options.Value.File}.");
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
                return StepResult.Failed($"Changelog version links in {options.Value.File} are missing or out of date.");
            }
        }


        report.Section("Release").Success($"Changelog entry for **{project.Version}** is present.");
        log.Detail($"Found changelog entry for {project.Version}.");
        return StepResult.Successful;
    }
}
