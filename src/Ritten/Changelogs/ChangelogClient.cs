using Ritten.Contracts.FileSystem;

namespace Ritten.Changelogs;

internal class ChangelogClient : IChangelog
{
    /// <inheritdoc />
    public async Task<Changelog> Read(IFile file, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(file.OpenRead());
        return Parse(await reader.ReadToEndAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ChangelogEntry> ReadEntry(IFile file, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(file.OpenRead());
        return ParseEntry(await reader.ReadToEndAsync(cancellationToken));
    }

    /// <inheritdoc />
    public Task Write(IFile file, Changelog changelog, CancellationToken cancellationToken = default) =>
        WriteText(file, Render(changelog), cancellationToken);

    /// <inheritdoc />
    public Task WriteEntry(IFile file, ChangelogEntry entry, CancellationToken cancellationToken = default) =>
        WriteText(file, RenderEntry(entry), cancellationToken);

    /// <inheritdoc />
    public Changelog Parse(string changelog) => ChangelogParser.Parse(changelog);

    /// <inheritdoc />
    public ChangelogEntry ParseEntry(string changelog) => ChangelogParser.ParseEntry(changelog);

    /// <inheritdoc />
    public string Render(Changelog changelog) => ChangelogRenderer.Render(changelog);

    /// <inheritdoc />
    public string RenderEntry(ChangelogEntry entry) => ChangelogRenderer.RenderEntry(entry);

    /// <inheritdoc />
    public IReadOnlyCollection<ChangelogLink> GenerateLinks(Changelog changelog, ChangelogRepository repository) =>
        ChangelogLinkGenerator.Generate(changelog, repository);

    private static async Task WriteText(IFile file, string text, CancellationToken cancellationToken)
    {
        var stream = file.OpenWrite();
        stream.SetLength(0); // OpenWrite isn't guaranteed to truncate an existing file.
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text.AsMemory(), cancellationToken);
    }
}
