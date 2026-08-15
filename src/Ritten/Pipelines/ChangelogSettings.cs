namespace Ritten.Pipelines;

/// <summary>
/// The <c>changelog</c> section of <c>ritten.json</c>.
/// </summary>
public sealed record ChangelogSettings
{
    /// <summary>
    /// The changelog file, relative to the project root.
    /// </summary>
    public string File { get; init; } = "CHANGELOG.md";
}
