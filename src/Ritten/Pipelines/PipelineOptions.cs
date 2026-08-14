namespace Ritten.Pipelines;

/// <summary>
/// The directory layout every pipeline step shares.
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// The directory build artifacts (e.g. packages) are written to, relative to the project root.
    /// </summary>
    public string ArtifactsDirectory { get; set; } = "artifacts";

    /// <summary>
    /// The directory intermediate pipeline output is written to, relative to the project root.
    /// </summary>
    public string TempDirectory { get; set; } = "temp";
}
