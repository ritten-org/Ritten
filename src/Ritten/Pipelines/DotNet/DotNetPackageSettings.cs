using Ritten.Core.Settings;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The <c>ritten.json</c> schema for the .NET package pipelines.
/// against.
/// </summary>
public sealed record DotNetPackageSettings
{
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
}
