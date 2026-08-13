using Ritten.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Fails the pipeline when the changelog has no entry for the version being shipped.
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's changelog options.</param>
/// <param name="release">The pipeline's release options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
public class ValidateChangelog(
    ILogger<ValidateChangelog> logger,
    IOptions<ChangelogOptions> options,
    IOptions<GitOptions> release,
    IPipelineContext context,
    IBuildReport report,
    IChangelog changelogs
) : IPipelineStep
{
    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.Skip)
        {
            logger.LogInformation("Skipping changelog check.");
            return StepResult.Successful;
        }

        var project = context.State.Get<Project>();
        if (project == null)
        {
            throw new Exception("Project info not found in state.");
        }

        var changelogFile = context.FileSystem.CurrentDirectory.GetFile(options.Value.File);
        if (!changelogFile.Exists)
        {
            report.Section("Release").Failure($"The changelog file `{options.Value.File}` does not exist.");
            throw new FileNotFoundException("Could not find changelog file", changelogFile.AbsolutePath);
        }

        var changelog = await changelogs.Read(changelogFile, cancellationToken);

        var isPrerelease = project.Version.IsPrerelease || project.Version < NuGetVersion.Parse("1.0.0");
        var entry = isPrerelease ? changelog.Unreleased : changelog.Entry(project.Version);
        if (entry is null)
        {
            report.Section("Release").Failure($"There's no changelog entry for **{project.Version}** in `{options.Value.File}`.");
            throw new Exception($"No changelog entry found for version {project.Version} in {options.Value.File}.");
        }

        if (entry.IsEmpty)
        {
            report.Section("Release").Failure($"The changelog entry for **{project.Version}** is empty.");
            throw new Exception($"Changelog entry for version {project.Version} is empty.");
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
                throw new Exception($"Changelog version links in {options.Value.File} are missing or out of date.");
            }
        }

        context.State.Set(entry);
        report.Section("Release").Success($"Changelog entry for **{project.Version}** is present.");
        logger.LogInformation("Found changelog entry for {Version}.", project.Version);
        return StepResult.Successful;
    }
}
