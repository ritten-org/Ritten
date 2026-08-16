namespace Ritten.Core;

/// <summary>
/// The settings every pipeline's <c>ritten.json</c> shares.
/// </summary>
public abstract record PipelineSettings
{
    /// <summary>
    /// The pipeline the project runs, e.g. <c>dotnet-tool</c>.
    /// </summary>
    public string? Pipeline { get; init; }
}
