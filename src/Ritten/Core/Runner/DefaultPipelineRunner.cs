using Microsoft.Extensions.Logging;
using Ritten.Contracts;

namespace Ritten.Core.Runner;

internal class DefaultPipelineRunner(
    ILogger<DefaultPipelineRunner> logger,
    IEnumerable<IProgressReporter> reporters,
    IEnumerable<IPipelineStep> steps,
    Pipeline pipeline
) : IPipelineRunner
{

    private readonly IReadOnlyCollection<IProgressReporter> _reporters = [.. reporters];
    private readonly IReadOnlyList<IPipelineStep> _steps = [.. steps];

    public async Task<PipelineResult> Run(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running pipeline...");

        await NotifyReporters(r => r.OnPipelineStarted(pipeline, cancellationToken));
        var steps = await RunSteps(cancellationToken);

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
        await NotifyReporters(r => r.OnPipelineCompleted(result, cancellationToken));

        logger.LogInformation("Pipeline finished with exit code {ExitCode}", result.ExitCode);
        return result;
    }

    private async Task<List<StepResult>> RunSteps(CancellationToken cancellationToken)
    {
        List<StepResult> results = [];
        foreach (var step in _steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(StepResult.StoppedAfterCancel);
                break;
            }

            await NotifyReporters(r => r.OnStepStarted(step, cancellationToken));

            var result = await RunStep(step, cancellationToken);
            results.Add(result);

            await NotifyReporters(r => r.OnStepCompleted(result, cancellationToken));

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
        try
        {
            return await step.Run(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled error during step. Exiting.");
            return StepResult.StoppedOnError;
        }
    }

    private async Task NotifyReporters(Func<IProgressReporter, Task> action)
    {
        foreach (var reporter in _reporters)
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
