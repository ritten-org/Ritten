namespace Wolfe.Hamelin.Changelogs;

/// <summary>
/// Exposes functionality for interacting with changelogs.
/// </summary>
public interface IChangelog
{
    /// <summary>
    /// Reads the changelog from the given file.
    /// </summary>
    public Task<Changelog> Read(string file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the changelog entry from the given file.
    /// </summary>
    public Task<ChangelogEntry> ReadEntry(string file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the changelog to the given file.
    /// </summary>
    public Task Write(string path, Changelog changelog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the changelog entry to the given file.
    /// </summary>
    public Task WriteEntry(string path, ChangelogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the given changelog.
    /// </summary>
    public Changelog Parse(string changelog);

    /// <summary>
    /// Parses the given changelog.
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
}
