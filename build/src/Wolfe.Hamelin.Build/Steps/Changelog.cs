using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Validate Changelog Entry")]
public class Changelog(
    ILogger<Changelog> logger,
    IOptions<BuildOptions> options,
    IPipelineContext context,
    IBuildReport report,
    IChangelog changelogs
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.SkipChangelog)
        {
            logger.LogInformation("Skipping changelog check.");
            return;
        }

        var project = context.State.Get<Project>();
        if (project == null)
        {
            throw new Exception("Project info not found in state.");
        }

        var changelogFile = context.FileSystem.CurrentDirectory.GetFile(options.Value.ChangelogFile);
        if (!changelogFile.Exists)
        {
            report.Section("Release").Failure($"The changelog file `{options.Value.ChangelogFile}` does not exist.");
            throw new FileNotFoundException("Could not find changelog file", changelogFile.AbsolutePath);
        }

        var changelog = await changelogs.Read(changelogFile.AbsolutePath, cancellationToken);

        var isPrerelease = project.Version.IsPrerelease || project.Version < NuGetVersion.Parse("1.0.0");
        var entry = isPrerelease ? changelog.Unreleased : changelog.Entry(project.Version);
        if (entry is null)
        {
            report.Section("Release").Failure($"There's no changelog entry for **{project.Version}** in `{options.Value.ChangelogFile}`.");
            throw new Exception($"No changelog entry found for version {project.Version} in {options.Value.ChangelogFile}.");
        }

        if (entry.IsEmpty)
        {
            report.Section("Release").Failure($"The changelog entry for **{project.Version}** is empty.");
            throw new Exception($"Changelog entry for version {project.Version} is empty.");
        }

        context.State.Set(entry);
        report.Section("Release").Success($"Changelog entry for **{project.Version}** is present.");
        logger.LogInformation("Found changelog entry for {Version}.", project.Version);
    }
}
