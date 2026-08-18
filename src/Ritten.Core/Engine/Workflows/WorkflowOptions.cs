namespace Ritten.Engine.Workflows;

/// <summary>
/// The directory layout every workflow step shares.
/// </summary>
public class WorkflowOptions
{
    /// <summary>
    /// The directory build artifacts (e.g. packages) are written to, relative to the project root.
    /// </summary>
    public string ArtifactsDirectory { get; set; } = "artifacts";

    /// <summary>
    /// The directory intermediate workflow output is written to, relative to the project root.
    /// </summary>
    public string TempDirectory { get; set; } = "temp";
}
