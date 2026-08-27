using NuGet.Versioning;
using Ritten.Releases;

namespace Ritten.Changelogs;

/// <summary>
/// Represents an entry in a <see cref="Changelog"/>.
/// </summary>
public record ChangelogEntry
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
    /// Takes on another entry's notes, keeping this entry's own first.
    /// </summary>
    /// <param name="other">The entry whose notes are being taken on.</param>
    public ChangelogEntry Merge(ChangelogEntry other)
    {
        var merged = this with
        {
            Preamble = Join(Preamble, other.Preamble),
            Added = [.. Added, .. other.Added],
            Changed = [.. Changed, .. other.Changed],
            Deprecated = [.. Deprecated, .. other.Deprecated],
            Removed = [.. Removed, .. other.Removed],
            Fixed = [.. Fixed, .. other.Fixed],
            Security = [.. Security, .. other.Security]
        };

        // Where the sections account for both bodies in full, dropping the bodies lets the entry
        // render as one set of sections rather than two — the same fixes under a single "Fixed".
        // Where they don't, the body is the only view that holds a heading the format doesn't
        // define, so the bodies are joined verbatim and an untidy repeat is the lesser loss.
        return IsStructured && other.IsStructured
            ? merged with { Body = "" }
            : merged with { Body = Join(Body, other.Body) };
    }

    /// <summary>
    /// What these notes do to what already shipped.
    /// </summary>
    public ReleaseKind ReleaseKind => this switch
    {
        { IsEmpty: true } => ReleaseKind.None,
        { Removed.Count: > 0 } or { Changed.Count: > 0 } => ReleaseKind.Breaking,
        { Added.Count: > 0 } or { Deprecated.Count: > 0 } => ReleaseKind.Features,
        _ => ReleaseKind.Fixes
    };

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

    /// <summary>
    /// Whether the sections account for the whole body, so nothing is lost by rebuilding it from them.
    /// </summary>
    /// <remarks>
    /// Compared line by line rather than as text, because the two differences a rebuild does make —
    /// the order the author wrote their sections in, and the blank lines between them — are
    /// formatting the format itself prescribes. A heading the structured view can't hold, and the
    /// notes under it, are lines that go missing, which is the loss this is looking for.
    /// </remarks>
    private bool IsStructured => Lines(ChangelogRenderer.RenderEntry(this with { Body = "" })).SetEquals(Lines(Body));

    private static HashSet<string> Lines(string text) =>
        [.. text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)];

    private static string Join(string first, string second) => (first.Trim('\n'), second.Trim('\n')) switch
    {
        ("", var only) => only,
        (var only, "") => only,
        var (a, b) => $"{a}\n\n{b}"
    };
}
