using NuGet.Versioning;

namespace Wolfe.Hamelin.Changelogs;

/// <summary>
/// Represents a changelog in the format described by https://keepachangelog.com/en/1.1.0/.
/// </summary>
public class Changelog
{
    /// <summary>
    /// Gets any preamble between the main header and the first version header.
    /// </summary>
    public string Preamble { get; init; } = "";

    /// <summary>
    /// Gets each version entry in the changelog.
    /// </summary>
    public IReadOnlyCollection<ChangelogEntry> Entries { get; init; } = [];

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
