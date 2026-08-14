namespace Ritten.Core.Settings;

/// <summary>
/// The <c>changelog</c> section of <c>ritten.json</c>.
/// </summary>
public sealed record ChangelogSettings
{
    /// <summary>
    /// The changelog file, relative to the project root.
    /// </summary>
    public string File { get; init; } = "CHANGELOG.md";

    /// <summary>
    /// The project's web URL. When set, the changelog's version links are validated against it.
    /// </summary>
    public string? Repository { get; init; }
}
