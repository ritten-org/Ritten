using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ritten.Core.Runner;

internal class PipelineHost(
    ILogger<PipelineHost> logger,
    IOptions<PipelineExecutionOptions> options,
    IHostApplicationLifetime lifetime,
    IPipelineRunner runner,
    PipelineExecutionSummaryStore summaryStore
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await runner.RunPipeline(cancellationToken);
            summaryStore.Summary = summary;
            if (options.Value.SetEnvironmentExitCodeOnCompletion)
            {
                Environment.ExitCode = summary.ExitCode;
            }
        }
        finally
        {
            if (options.Value.StopApplicationOnCompletion)
            {
                logger.LogInformation("Requesting application stop...");
                lifetime.StopApplication();
            }
        }
    }
}
