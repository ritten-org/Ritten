namespace Ritten.Changelogs;

/// <summary>
/// Describes the repository a changelog's version links point at.
/// </summary>
/// <param name="Url">The base web URL of the repository, e.g. <c>https://github.com/owner/repo</c>.</param>
public record ChangelogRepository(string Url)
{
    /// <summary>
    /// The prefix used when tagging releases (defaults to <c>v</c>, so version 1.2.0 is tag <c>v1.2.0</c>).
    /// </summary>
    public string TagPrefix { get; init; } = "v";
}
