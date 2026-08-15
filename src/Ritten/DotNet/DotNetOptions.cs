namespace Ritten.DotNet;

/// <summary>
/// Settings shared by the .NET pipeline steps. Bound from the <c>DotNet</c> configuration section.
/// </summary>
public class DotNetOptions
{
    /// <summary>
    /// The build configuration used to build, test, and pack.
    /// </summary>
    public string Configuration { get; set; } = "Release";

    /// <summary>
    /// The project file of the package being shipped, relative to the repository root.
    /// </summary>
    public string ProjectFile { get; set; } = "";

    /// <summary>
    /// The repository's web URL, when configured explicitly; wins over anything derived.
    /// </summary>
    public string? Repository { get; set; }
}
