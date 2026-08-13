namespace Ritten.Core.Runner;

internal interface IPipelineRunner
{
    Task<PipelineExecutionSummary> RunPipeline(CancellationToken cancellationToken);
}
