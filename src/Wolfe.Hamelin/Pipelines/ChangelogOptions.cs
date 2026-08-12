namespace Wolfe.Hamelin.Pipelines;

/// <summary>
/// Settings for changelog validation. Bound from the <c>Changelog</c> configuration section.
/// </summary>
public class ChangelogOptions
{
    /// <summary>
    /// The changelog file, relative to the repository root.
    /// </summary>
    public string File { get; set; } = "CHANGELOG.md";

    /// <summary>
    /// The repository's web URL; when set, the changelog's version links are validated against it.
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Skips changelog validation entirely (e.g. for dependabot pull requests).
    /// </summary>
    public bool Skip { get; set; }
}
