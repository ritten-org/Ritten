namespace Ritten.Changelogs;

/// <summary>
/// A reference-style link definition from the end of a changelog, e.g. <c>[1.2.0]: https://github.com/owner/repo/compare/v1.1.0...v1.2.0</c>.
/// </summary>
/// <param name="Label">The link label: a version number or <c>Unreleased</c>.</param>
/// <param name="Url">The link target.</param>
public record ChangelogLink(string Label, string Url)
{
    /// <summary>
    /// Renders the link as a markdown reference-style link definition.
    /// </summary>
    public string ToMarkdown() => $"[{Label}]: {Url}";
}
