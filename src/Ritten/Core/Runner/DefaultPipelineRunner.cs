using Ritten.Contracts;

namespace Ritten.Core.Runner;

internal class DefaultPipelineRunner(
    IPipelineLog log,
    IEnumerable<IProgressReporter> reporters,
    IEnumerable<IPipelineStep> steps,
    Pipeline pipeline
) : IPipelineRunner
{

    private readonly IReadOnlyCollection<IProgressReporter> _reporters = [.. reporters];
    private readonly IReadOnlyList<IPipelineStep> _steps = [.. steps];

    public async Task<PipelineResult> Run(CancellationToken cancellationToken)
    {
        await NotifyReporters(r => r.OnPipelineStarted(pipeline, cancellationToken));
        var stepResults = await RunSteps(cancellationToken);

        var exitCode = cancellationToken.IsCancellationRequested
            ? PipelineExitCodes.Cancelled
            : stepResults.FirstOrDefault(s => s.IsFailure)?.ExitCode ?? PipelineExitCodes.Success;

        var result = new PipelineResult(exitCode, stepResults);
        await NotifyReporters(r => r.OnPipelineCompleted(result, cancellationToken));

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

            await NotifyReporters(r => r.OnStepCompleted(step, result, cancellationToken));

            if (!result.Continue)
            {
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StepResult.StoppedAfterCancel;
        }
        catch (Exception ex)
        {
            log.Verbose(ex.ToString());
            return StepResult.Failed(ex.Message);
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
                log.Warning($"Progress reporter {reporter.GetType().Name} failed: {ex.Message}");
                log.Verbose(ex.ToString());
            }
        }
    }
}
