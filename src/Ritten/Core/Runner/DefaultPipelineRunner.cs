using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Core.Extensions;
using Ritten.Core.Steps;

namespace Ritten.Core.Runner;

internal class DefaultPipelineRunner(ILogger<DefaultPipelineRunner> logger, IServiceScopeFactory scopeFactory) : IPipelineRunner
{
    public async Task<PipelineExecutionSummary> RunPipeline(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running pipeline...");

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IPipelineContext>();
        var reporters = scope.ServiceProvider.GetServices<IProgressReporter>().ToList();

        await NotifyReporters(reporters, r => r.OnPipelineStarted(cancellationToken));
        var results = await RunSteps(scope, reporters, cancellationToken);
        var summary = new PipelineExecutionSummary(context, results, cancellationToken);
        await NotifyReporters(reporters, r => r.OnPipelineCompleted(summary.ExitCode, summary.Steps, cancellationToken));

        logger.LogInformation("Pipeline finished with exit code {ExitCode}", summary.ExitCode);
        return summary;
    }

    private async Task<IEnumerable<StepExecutionSummary>> RunSteps(AsyncServiceScope scope, List<IProgressReporter> reporters, CancellationToken cancellationToken)
    {
        List<StepExecutionSummary> summaries = [];
        var stepProvider = scope.ServiceProvider.GetRequiredService<IPipelineStepProvider>();
        var steps = stepProvider.GetSteps();
        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                summaries.Add(new StepExecutionSummary(step.GetDisplayName(), PipelineStepResult.StoppedAfterCancel));
                break;
            }

            var displayName = step.GetDisplayName();
            await NotifyReporters(reporters, r => r.OnStepStarted(displayName, cancellationToken));

            var summary = await RunStep(step, cancellationToken);
            summaries.Add(summary);

            await NotifyReporters(reporters, r => r.OnStepCompleted(summary, cancellationToken));

            if (!summary.Result.Continue)
            {
                logger.LogInformation("Step resulted in non-continuation. Aborting pipeline.");
                break;
            }
        }

        return summaries.ToArray();
    }

    private async Task<StepExecutionSummary> RunStep(IPipelineStep step, CancellationToken cancellationToken)
    {
        var displayName = step.GetDisplayName();
        try
        {
            await step.Run(cancellationToken);
            return new StepExecutionSummary(displayName, PipelineStepResult.Successful);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled error during step. Exiting.");
            return new StepExecutionSummary(displayName, PipelineStepResult.StoppedOnError(ex));
        }
    }

    private async Task NotifyReporters(List<IProgressReporter> reporters, Func<IProgressReporter, Task> action)
    {
        foreach (var reporter in reporters)
        {
            try
            {
                await action(reporter);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Progress reporter error. Continuing...");
            }
        }
    }
}
