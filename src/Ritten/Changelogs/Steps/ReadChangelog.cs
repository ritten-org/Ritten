using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Reads and parses the changelog file.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("read changelog", StepKind.Work)]
public class ReadChangelog(IWorkflowLog log, IFileSystem fileSystem, IWorkflowReport report, IChangelog changelogs)
{
    /// <summary>
    /// Reads the configured changelog file.
    /// </summary>
    /// <param name="changelogFile">The changelog the project keeps.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<Changelog>> Run(ChangelogSettings changelogFile, CancellationToken cancellationToken = default)
    {
        var changelog = fileSystem.ProjectRoot.GetFile(changelogFile.File);
        if (changelog.Exists)
        {
            var parsed = await changelogs.Read(changelog, cancellationToken);
            log.Detail($"Read {changelogFile.File} ({parsed.Entries.Count} {(parsed.Entries.Count == 1 ? "entry" : "entries")}).");
            return parsed;
        }

        report.Section(SectionName.Changelog).Failure("The changelog file does not exist.");
        return StepResult.Failed($"Could not find changelog file '{changelogFile.File}'.");
    }
}
