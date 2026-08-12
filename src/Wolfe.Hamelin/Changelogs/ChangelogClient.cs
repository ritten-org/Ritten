namespace Wolfe.Hamelin.Changelogs;

internal class ChangelogClient : IChangelog
{
    /// <inheritdoc />
    public async Task<Changelog> Read(string file, CancellationToken cancellationToken = default)
    {
        var changelog = await File.ReadAllTextAsync(file, cancellationToken);
        return Parse(changelog);
    }

    /// <inheritdoc />
    public async Task<ChangelogEntry> ReadEntry(string file, CancellationToken cancellationToken = default)
    {
        var entry = await File.ReadAllTextAsync(file, cancellationToken);
        return ParseEntry(entry);
    }

    /// <inheritdoc />
    public Task Write(string path, Changelog changelog, CancellationToken cancellationToken = default)
    {
        var text = Render(changelog);
        return File.WriteAllTextAsync(path, text, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteEntry(string path, ChangelogEntry entry, CancellationToken cancellationToken = default)
    {
        var text = RenderEntry(entry);
        return File.WriteAllTextAsync(path, text, cancellationToken);
    }

    /// <inheritdoc />
    public Changelog Parse(string changelog) => ChangelogParser.Parse(changelog);

    /// <inheritdoc />
    public ChangelogEntry ParseEntry(string changelog) => ChangelogParser.ParseEntry(changelog);

    /// <inheritdoc />
    public string Render(Changelog changelog) => ChangelogRenderer.Render(changelog);

    /// <inheritdoc />
    public string RenderEntry(ChangelogEntry entry) => ChangelogRenderer.RenderEntry(entry);
}
