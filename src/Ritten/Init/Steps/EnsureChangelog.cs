using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Init.Steps;

/// <summary>
/// Makes sure the repository has a changelog with somewhere to write the next release's notes.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="fileSystem">The file system.</param>
/// <param name="changelogs">The changelog client.</param>
[Step("ensure changelog", StepKind.Work)]
public class EnsureChangelog(IWorkflowLog log, IFileSystem fileSystem, IChangelog changelogs)
{
    /// <summary>
    /// What a changelog says before anybody has released anything.
    /// </summary>
    private const string Preamble =
        """
        # Changelog

        All notable changes to this project will be documented in this file.

        The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
        """;

    /// <summary>
    /// Writes a changelog when there isn't one, and gives an existing one an unreleased section
    /// when it hasn't got one. Everything already written stays exactly as it was written.
    /// </summary>
    /// <param name="changelogFile">The changelog the project keeps.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(ChangelogSettings changelogFile, CancellationToken ct = default)
    {
        var file = fileSystem.ProjectRoot.GetFile(changelogFile.File);
        if (!file.Exists)
        {
            await changelogs.Write(file, new Changelog { Preamble = Preamble, Entries = [new ChangelogEntry()] }, ct);
            log.Detail($"{changelogFile.File}: a changelog, with somewhere to write the next release's notes.");
            return StepResult.Successful;
        }

        var changelog = await changelogs.Read(file, ct);
        if (changelog.Unreleased is not null)
        {
            log.Skipped($"{changelogFile.File} already has somewhere to write the next release's notes.");
            return StepResult.Successful;
        }

        // The unreleased notes go at the top, above every version that has already shipped.
        await changelogs.Write(file, changelog with { Entries = [new ChangelogEntry(), .. changelog.Entries] }, ct);
        log.Detail($"{changelogFile.File}: an unreleased section, above everything already released.");
        return StepResult.Successful;
    }
}
