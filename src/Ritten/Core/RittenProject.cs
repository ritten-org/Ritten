namespace Ritten.Core;

/// <summary>
/// The definition of a Ritten build project.
/// </summary>
internal sealed partial class RittenProject
{
    /// <summary>
    /// The project file of the package being shipped, relative to the project root.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The directory the project is located in.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// The build configuration used to build, test, and pack.
    /// </summary>
    public required string Configuration { get; init; }

    /// <summary>
    /// The changelog file, relative to the project root.
    /// </summary>
    public required string Changelog { get; init; }

    /// <summary>
    /// The project's web URL. When set, the changelog's version links are validated against it.
    /// </summary>
    public string? Repository { get; init; }

    /// <summary>
    /// The prefix for release tag names, so that tags are <c>TagPrefix + version</c>, e.g. <c>v1.2.0</c>.
    /// </summary>
    public required string TagPrefix { get; init; }

    /// <summary>
    /// The V3 index URL of the NuGet feed the package is validated against and published to.
    /// </summary>
    public string Feed { get; init; } = "https://api.nuget.org/v3/index.json";
}
