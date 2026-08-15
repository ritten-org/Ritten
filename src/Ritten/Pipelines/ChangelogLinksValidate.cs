using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Pipelines.Git;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Keeps the changelog's version links correct.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's changelog options.</param>
/// <param name="release">The pipeline's release options.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("changelog links", StepKind.Validation)]
public class ChangelogLinksValidate(IPipelineLog log, IOptions<ChangelogOptions> options, IOptions<GitOptions> release, IBuildReport report, IChangelog changelogs)
{
    /// <summary>
    /// Validates the changelog's version links.
    /// </summary>
    /// <param name="changelog">The parsed changelog (see <see cref="ReadChangelog"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public Task<StepResult> Run(Changelog changelog, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.Value.RepositoryUrl))
        {
            log.Skipped("No repository URL configured; links not validated.");
            return Task.FromResult(StepResult.Successful);
        }

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

        log.Detail("The version links are up to date.");
        return Task.FromResult(StepResult.Successful);
    }
}
