namespace Wolfe.Hamelin.Build.Models;

// Bound from the "Changelog" section.
public class ChangelogOptions
{
    public string File { get; set; } = "CHANGELOG.md";

    /// <summary>
    /// The repository's web URL; when set, the changelog's version links are validated against it.
    /// </summary>
    public string? RepositoryUrl { get; set; }

    public bool Skip { get; set; }
}
