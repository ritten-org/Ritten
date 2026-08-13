namespace Ritten.Core.Runner;

/// <summary>
/// Exposes an interface for running pipelines.
/// </summary>
internal interface IPipelineRunner
{
    /// <summary>
    /// Runs the current pipeline.
    /// </summary>
    Task<PipelineResult> RunPipeline(CancellationToken cancellationToken);
}
