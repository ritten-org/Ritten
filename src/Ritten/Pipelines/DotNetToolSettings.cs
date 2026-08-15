namespace Ritten.Pipelines;

/// <summary>
/// The <c>ritten.json</c> schema for the .NET package pipelines.
/// against.
/// </summary>
public sealed record DotNetToolSettings
{
    /// <summary>
    /// The repository's web URL; read from the project file or the origin remote when not set.
    /// </summary>
    public string? Repository { get; init; }

    /// <summary>
    /// What to build, and how.
    /// </summary>
    public DotNetBuildSettings Build { get; init; } = new();

    /// <summary>
    /// The changelog to validate.
    /// </summary>
    public ChangelogSettings Changelog { get; init; } = new();

    /// <summary>
    /// How a release is tagged and where it's published.
    /// </summary>
    public ReleaseSettings Release { get; init; } = new();

    /// <summary>
    /// Code coverage collection and thresholds.
    /// </summary>
    public CoverageSettings Coverage { get; init; } = new();
}
