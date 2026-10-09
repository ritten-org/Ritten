using System.Text.Json.Serialization;

namespace Ritten.DotNet;

/// <summary>
/// What to build, and how: the <c>build</c> section of <c>ritten.json</c> for .NET projects.
/// </summary>
public sealed record DotNetBuildSettings
{
    /// <summary>
    /// The project file of the package being shipped, relative to the project root.
    /// Set either this or <see cref="Projects"/>.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The project files of every package the repository ships, relative to the project root.
    /// The first project is the metadata source.
    /// </summary>
    public IReadOnlyList<string>? Projects { get; init; }

    /// <summary>
    /// The build configuration used to build, test, and pack.
    /// </summary>
    public string Configuration { get; init; } = "Release";

    /// <summary>
    /// Every project the repository ships, whichever of the two spellings declares them; the first is the metadata
    /// source.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> ShippedProjects => Projects is { Count: > 0 } ? Projects : Project != null ? [Project] : [];
}
