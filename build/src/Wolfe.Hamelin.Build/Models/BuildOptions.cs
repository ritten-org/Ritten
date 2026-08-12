namespace Wolfe.Hamelin.Build.Models;

// Bound from the "Build" section: the core layout every step shares.
public class BuildOptions
{
    public string ArtifactsDirectory { get; set; } = "artifacts";
    public string TempDirectory { get; set; } = "temp";
    public string Configuration { get; set; } = "Release";
    public string ProjectFile { get; set; } = "";
}
