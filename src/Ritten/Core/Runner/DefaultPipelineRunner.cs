using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core.Runner;

internal class DefaultPipelineRunner(
    IPipelineLog log,
    IEnumerable<IProgressReporter> reporters,
    IReadOnlyList<Step> steps,
    IServiceProvider services,
    PipelineJob job
) : IPipelineRunner
{

    private readonly IReadOnlyCollection<IProgressReporter> _reporters = [.. reporters];

    public async Task<PipelineResult> Run(CancellationToken cancellationToken)
    {
        await NotifyReporters(r => r.OnPipelineStarted(job, cancellationToken));
        var stepResults = await RunSteps(cancellationToken);

        var exitCode = cancellationToken.IsCancellationRequested
            ? PipelineExitCodes.Cancelled
            : stepResults.FirstOrDefault(s => s.IsFailure)?.ExitCode ?? PipelineExitCodes.Success;

        var result = new PipelineResult(exitCode, stepResults);
        await NotifyReporters(r => r.OnPipelineCompleted(result, cancellationToken), reverse: true);

        return result;
    }

    private async Task<List<StepResult>> RunSteps(CancellationToken cancellationToken)
    {
        // The values steps produce, living exactly as long as the run that produced them.
        Dictionary<Type, object> state = [];
        List<StepResult> results = [];
        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(StepResult.StoppedAfterCancel);
                break;
            }

            await NotifyReporters(r => r.OnStepStarted(step, cancellationToken));

            var result = await RunStep(step, state, cancellationToken);
            results.Add(result);

            await NotifyReporters(r => r.OnStepCompleted(step, result, cancellationToken));

            if (!result.Continue)
            {
                break;
            }
        }

        return results;
    }

    private async Task<StepResult> RunStep(Step step, Dictionary<Type, object> state, CancellationToken cancellationToken)
    {
        try
        {
            var instance = services.GetRequiredService(step.StepType);
            return await step.Invoke(instance, state, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StepResult.StoppedAfterCancel;
        }
        catch (Exception ex)
        {
            log.Verbose("Unhandled error running step", ex);
            return StepResult.Failed(ex.Message);
        }
    }

    private async Task NotifyReporters(Func<IProgressReporter, Task> action, bool reverse = false)
    {
        var reporters = reverse ? _reporters.Reverse() : _reporters;
        foreach (var reporter in reporters)
        {
            try
            {
                await action(reporter);
            }
            catch (Exception ex)
            {
                log.Warning($"Progress reporter {reporter.GetType().Name} failed.", ex);
            }
        }
    }
}
