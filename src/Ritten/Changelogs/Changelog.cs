using NuGet.Versioning;

namespace Ritten.Changelogs;

/// <summary>
/// Represents a changelog in the format described by https://keepachangelog.com/en/1.1.0/.
/// </summary>
public record Changelog
{
    /// <summary>
    /// Gets everything before the first version heading, verbatim.
    /// </summary>
    public string Preamble { get; init; } = "";

    /// <summary>
    /// Gets each version entry in the changelog.
    /// </summary>
    public IReadOnlyCollection<ChangelogEntry> Entries { get; init; } = [];

    /// <summary>
    /// Gets the reference-style version links from the end of the file.
    /// </summary>
    public IReadOnlyCollection<ChangelogLink> Links { get; init; } = [];

    /// <summary>
    /// Gets the unreleased section if there is one.
    /// </summary>
    public ChangelogEntry? Unreleased => Entries.FirstOrDefault(v => v.Version == null);

    /// <summary>
    /// Gets the entry for the given version.
    /// </summary>
    public ChangelogEntry? Entry(NuGetVersion version)
    {
        return Entries.FirstOrDefault(v => v.Version == version);
    }
}
