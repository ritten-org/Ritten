using Hamelin.FileSystem;

namespace Ritten.Changelogs;

/// <summary>
/// Exposes functionality for interacting with changelogs.
/// </summary>
public interface IChangelog
{
    /// <summary>
    /// Reads the changelog from the given file.
    /// </summary>
    public Task<Changelog> Read(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single changelog entry from the given file.
    /// </summary>
    public Task<ChangelogEntry> ReadEntry(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the changelog to the given file, replacing its contents.
    /// </summary>
    public Task Write(IFile file, Changelog changelog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the changelog entry to the given file, replacing its contents.
    /// </summary>
    public Task WriteEntry(IFile file, ChangelogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the given changelog.
    /// </summary>
    public Changelog Parse(string changelog);

    /// <summary>
    /// Parses the given changelog entry.
    /// </summary>
    public ChangelogEntry ParseEntry(string changelog);

    /// <summary>
    /// Renders the given changelog.
    /// </summary>
    public string Render(Changelog changelog);

    /// <summary>
    /// Renders the given changelog entry.
    /// </summary>
    public string RenderEntry(ChangelogEntry entry);

    /// <summary>
    /// Computes the reference-style version links the changelog should have for the given repository.
    /// </summary>
    public IReadOnlyCollection<ChangelogLink> GenerateLinks(Changelog changelog, ChangelogRepository repository);
}
