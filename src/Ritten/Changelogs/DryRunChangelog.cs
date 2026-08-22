using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Changelogs;

/// <summary>
/// Reports what would be written instead of writing it. Reading, parsing, and rendering pass
/// through, so a rehearsal still narrates exactly what the file would say.
/// </summary>
internal class DryRunChangelog(IWorkflowLog log, IChangelog inner) : IChangelog
{
    /// <inheritdoc />
    public Task<Changelog> Read(IFile file, CancellationToken cancellationToken = default) =>
        inner.Read(file, cancellationToken);

    /// <inheritdoc />
    public Task<ChangelogEntry> ReadEntry(IFile file, CancellationToken cancellationToken = default) =>
        inner.ReadEntry(file, cancellationToken);

    /// <inheritdoc />
    public Task Write(IFile file, Changelog changelog, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would write {file.Name}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteEntry(IFile file, ChangelogEntry entry, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would write {file.Name}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Changelog Parse(string changelog) => inner.Parse(changelog);

    /// <inheritdoc />
    public ChangelogEntry ParseEntry(string changelog) => inner.ParseEntry(changelog);

    /// <inheritdoc />
    public string Render(Changelog changelog) => inner.Render(changelog);

    /// <inheritdoc />
    public string RenderEntry(ChangelogEntry entry) => inner.RenderEntry(entry);

    /// <inheritdoc />
    public IReadOnlyCollection<ChangelogLink> GenerateLinks(Changelog changelog, ChangelogRepository repository) =>
        inner.GenerateLinks(changelog, repository);
}
