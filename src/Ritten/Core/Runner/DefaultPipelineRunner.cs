using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Core.Extensions;
using Ritten.Core.Steps;

namespace Ritten.Core.Runner;

internal class DefaultPipelineRunner(ILogger<DefaultPipelineRunner> logger, IServiceScopeFactory scopeFactory) : IPipelineRunner
{
    public async Task<PipelineResult> RunPipeline(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running pipeline...");

        await using var scope = scopeFactory.CreateAsyncScope();
        var reporters = scope.ServiceProvider.GetServices<IProgressReporter>().ToList();

        await NotifyReporters(reporters, r => r.OnPipelineStarted(cancellationToken));
        var steps = await RunSteps(scope, reporters, cancellationToken);

        int exitCode;
        if (cancellationToken.IsCancellationRequested)
        {
            exitCode = PipelineExitCodes.StoppedAfterCancel;
        }
        else if (steps.Count == 0)
        {
            exitCode = PipelineExitCodes.Success;
        }
        else
        {
            exitCode = steps[^1].ExitCode;
        }
        var result = new PipelineResult(exitCode, steps);
        await NotifyReporters(reporters, r => r.OnPipelineCompleted(result.ExitCode, result, cancellationToken));

        logger.LogInformation("Pipeline finished with exit code {ExitCode}", result.ExitCode);
        return result;
    }

    private async Task<List<StepResult>> RunSteps(AsyncServiceScope scope, List<IProgressReporter> reporters, CancellationToken cancellationToken)
    {
        List<StepResult> results = [];
        var stepProvider = scope.ServiceProvider.GetRequiredService<IPipelineStepProvider>();
        var steps = stepProvider.GetSteps();
        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(StepResult.StoppedAfterCancel);
                break;
            }

            var displayName = step.GetDisplayName();
            await NotifyReporters(reporters, r => r.OnStepStarted(displayName, cancellationToken));

            var result = await RunStep(step, cancellationToken);
            results.Add(result);

            await NotifyReporters(reporters, r => r.OnStepCompleted(result, cancellationToken));

            if (!result.Continue)
            {
                logger.LogInformation("Step resulted in non-continuation. Aborting pipeline.");
                break;
            }
        }

        return results;
    }

    private async Task<StepResult> RunStep(IPipelineStep step, CancellationToken cancellationToken)
    {
        var displayName = step.GetDisplayName();
        try
        {
            await step.Run(cancellationToken);
            return StepResult.Successful;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled error during step. Exiting.");
            return StepResult.StoppedOnError(ex);
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
