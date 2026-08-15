namespace Ritten.Pipelines;

/// <summary>
/// The <c>build</c> section of <c>ritten.json</c> for .NET projects.
/// </summary>
public sealed record DotNetBuildSettings
{
    /// <summary>
    /// The project file of the package being shipped, relative to the project root.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The build configuration used to build, test, and pack.
    /// </summary>
    public string Configuration { get; init; } = "Release";
}
