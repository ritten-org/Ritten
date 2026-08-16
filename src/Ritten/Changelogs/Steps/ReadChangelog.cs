using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Reads and parses the changelog file.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's changelog options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("read changelog", StepKind.Work)]
public class ReadChangelog(IPipelineLog log, IOptions<ChangelogOptions> options, IFileSystem fileSystem, IBuildReport report, IChangelog changelogs)
{
    /// <summary>
    /// Reads the configured changelog file.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<Changelog>> Run(CancellationToken cancellationToken = default)
    {
        var changelog = fileSystem.ProjectRoot.GetFile(options.Value.File);
        if (changelog.Exists)
        {
            var parsed = await changelogs.Read(changelog, cancellationToken);
            log.Detail($"Read {options.Value.File} ({parsed.Entries.Count} {(parsed.Entries.Count == 1 ? "entry" : "entries")}).");
            return parsed;
        }

        report.Section("Changelog").Failure("The changelog file does not exist.");
        return StepResult.Failed($"Could not find changelog file '{options.Value.File}'.");
    }
}
