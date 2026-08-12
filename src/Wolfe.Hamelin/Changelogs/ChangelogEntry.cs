using NuGet.Versioning;

namespace Wolfe.Hamelin.Changelogs;

/// <summary>
/// Represents an entry in a <see cref="Changelog"/>.
/// </summary>
public class ChangelogEntry
{
    /// <summary>
    /// Gets the version that this entry describes the changes for, or <c>null</c> if the changes are unreleased.
    /// </summary>
    public NuGetVersion? Version { get; init; }

    /// <summary>
    /// Gets the date the version was released, if it has been released.
    /// </summary>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// The raw markdown content of the entry, exactly as authored.
    /// </summary>
    public string Body { get; init; } = "";

    /// <summary>
    /// Any markdown between the version heading and the first section heading.
    /// </summary>
    public string Preamble { get; init; } = "";

    /// <summary>
    /// New features.
    /// </summary>
    public IReadOnlyCollection<string> Added { get; init; } = [];

    /// <summary>
    /// Changes to existing functionality.
    /// </summary>
    public IReadOnlyCollection<string> Changed { get; init; } = [];

    /// <summary>
    /// Soon-to-be removed features.
    /// </summary>
    public IReadOnlyCollection<string> Deprecated { get; init; } = [];

    /// <summary>
    /// Removed features.
    /// </summary>
    public IReadOnlyCollection<string> Removed { get; init; } = [];

    /// <summary>
    /// Bug fixes.
    /// </summary>
    public IReadOnlyCollection<string> Fixed { get; init; } = [];

    /// <summary>
    /// Vulnerability fixes.
    /// </summary>
    public IReadOnlyCollection<string> Security { get; init; } = [];

    /// <summary>
    /// True if the entry contains no notes. Otherwise, false.
    /// </summary>
    public bool IsEmpty =>
        Added.Count == 0
        && Changed.Count == 0
        && Removed.Count == 0
        && Fixed.Count == 0
        && Deprecated.Count == 0
        && Security.Count == 0
        && string.IsNullOrWhiteSpace(Preamble)
        && string.IsNullOrWhiteSpace(Body);
}
