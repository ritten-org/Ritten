using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Reads and parses the changelog file.
/// </summary>
/// <param name="options">The pipeline's changelog options.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="report">The build report.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("read changelog", StepKind.Work)]
public class ReadChangelog(IOptions<ChangelogOptions> options, IFileSystem fileSystem, IBuildReport report, IChangelog changelogs)
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
            return await changelogs.Read(changelog, cancellationToken);
        }

        report.Section("Release").Failure("The changelog file does not exist.");
        return StepResult.Failed($"Could not find changelog file '{options.Value.File}'.");
    }
}
