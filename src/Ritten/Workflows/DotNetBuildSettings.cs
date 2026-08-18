namespace Ritten.Workflows;

/// <summary>
/// The <c>build</c> section of <c>ritten.json</c> for .NET projects.
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
}
