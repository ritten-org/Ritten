using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Engine.Runs;

internal class DefaultWorkflowRunner(
    IWorkflowLog log,
    IEnumerable<IWorkflowProgress> reporters,
    IReadOnlyList<Step> steps,
    IServiceProvider services,
    WorkflowJob job
) : IWorkflowRunner
{

    private readonly IReadOnlyCollection<IWorkflowProgress> _reporters = [.. reporters];

    public async Task<WorkflowResult> Run(CancellationToken cancellationToken)
    {
        await NotifyReporters(r => r.OnWorkflowStarted(job, cancellationToken));
        var outcomes = await RunSteps(cancellationToken);

        var exitCode = cancellationToken.IsCancellationRequested
            ? ExitCode.Cancelled
            : outcomes.FirstOrDefault(o => o.Result.IsFailure)?.Result.ExitCode ?? ExitCode.Success;

        var result = new WorkflowResult(exitCode, outcomes);
        await NotifyReporters(r => r.OnWorkflowCompleted(result, cancellationToken), reverse: true);

        return result;
    }

    private async Task<List<StepOutcome>> RunSteps(CancellationToken cancellationToken)
    {
        // The values steps produce, living exactly as long as the run that produced them.
        Dictionary<Type, object> state = [];
        List<StepOutcome> results = [];
        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(new StepOutcome(step, StepResult.StoppedAfterCancel));
                break;
            }

            await NotifyReporters(r => r.OnStepStarted(step, cancellationToken));

            var result = await RunStep(step, state, cancellationToken);
            results.Add(new StepOutcome(step, result));

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

    private async Task NotifyReporters(Func<IWorkflowProgress, Task> action, bool reverse = false)
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
