namespace Ritten.Changelogs;

/// <summary>
/// Settings for changelog validation.
/// </summary>
public class ChangelogOptions
{
    /// <summary>
    /// The changelog file, relative to the repository root.
    /// </summary>
    public string File { get; set; } = "CHANGELOG.md";
}
