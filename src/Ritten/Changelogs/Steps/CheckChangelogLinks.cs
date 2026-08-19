using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Git;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Keeps the changelog's version links correct.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's changelog options.</param>
/// <param name="release">The workflow's release options.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("check changelog links", StepKind.Check)]
public class CheckChangelogLinks(IWorkflowLog log, IOptions<ChangelogOptions> options, IOptions<GitOptions> release, IWorkflowReport report, IChangelog changelogs)
{
    /// <summary>
    /// Validates the changelog's version links.
    /// </summary>
    /// <param name="project">The project the changelog belongs to (see <see cref="DotNet.Steps.ResolveRelease"/>).</param>
    /// <param name="changelog">The parsed changelog (see <see cref="ReadChangelog"/>).</param>
    public StepResult Run(Project project, Changelog changelog)
    {
        if (string.IsNullOrEmpty(project.Repository))
        {
            log.Skipped("No repository URL configured or derivable; links not validated.");
            return StepResult.Successful;
        }

        log.Verbose($"Validating links against {project.Repository}.");
        var repository = new ChangelogRepository(project.Repository) { TagPrefix = release.Value.TagPrefix };
        var expected = changelogs.GenerateLinks(changelog, repository);
        if (!changelog.Links.SequenceEqual(expected))
        {
            var block = string.Join('\n', expected.Select(l => l.ToMarkdown()));
            report.Section("Changelog")
                .Failure($"The version links in `{options.Value.File}` are missing or out of date. Replace the link block at the bottom of the file with:\n```\n{block}\n```");

            return new Error($"The version links in {options.Value.File} are missing or out of date. Expected:")
            {
                Verbatim = block
            };
        }

        log.Detail("The version links are up to date.");
        return StepResult.Successful;
    }
}
