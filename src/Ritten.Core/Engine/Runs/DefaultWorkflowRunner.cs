using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Engine.Runs;

internal class DefaultWorkflowRunner(
    IWorkflowLog log,
    IEnumerable<IWorkflowProgress> reporters,
    IReadOnlyList<Step> steps,
    IServiceProvider services,
    WorkflowJob job,
    RunContext context
) : IWorkflowRunner
{

    private readonly IReadOnlyCollection<IWorkflowProgress> _reporters = [.. reporters];

    public async Task<WorkflowResult> Run(CancellationToken cancellationToken)
    {
        // The job's span opens before the reporters hear the job start and closes after they hear it end, so a
        // reporter finds it current and can add what the host knows to it.
        using var activity = StartJob();
        await NotifyReporters(r => r.OnWorkflowStarted(job, cancellationToken));
        var outcomes = await RunSteps(cancellationToken);

        var exitCode = cancellationToken.IsCancellationRequested
            ? ExitCode.Cancelled
            : outcomes.FirstOrDefault(o => o.Result.IsFailure)?.Result.ExitCode ?? ExitCode.Success;

        var result = new WorkflowResult(exitCode, outcomes);
        CompleteJob(activity, result);
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

            // As the job's: current for the reporters either side of the step, and the parent of what it does.
            using var activity = StartStep(step);
            await NotifyReporters(r => r.OnStepStarted(step, cancellationToken));

            var result = await RunStep(step, state, cancellationToken);
            results.Add(new StepOutcome(step, result));
            CompleteStep(activity, result);

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
            Activity.Current?.AddException(ex);
            log.Verbose("Unhandled error running step", ex);
            return StepResult.Failed(ex.Message);
        }
    }

    private Activity? StartJob()
    {
        var activity = RittenTelemetry.Source.StartActivity($"{job.Workflow} {job.Name}");
        if (activity is null)
        {
            return null;
        }

        activity.SetTag(RittenTelemetry.Workflow, job.Workflow);
        activity.SetTag(RittenTelemetry.Job, job.Name);
        activity.SetTag(RittenTelemetry.DryRun, job.DryRun);

        // A run outside CI has no pipeline to name, only the engine's default title.
        if (context.Id is { } id)
        {
            activity.SetTag(RittenTelemetry.PipelineName, context.Title);
            activity.SetTag(RittenTelemetry.PipelineRunId, id);
            activity.SetTag(RittenTelemetry.PipelineRunUrl, context.Url);
        }

        return activity;
    }

    private static void CompleteJob(Activity? activity, WorkflowResult result)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(RittenTelemetry.RunExitCode, result.ExitCode.Value);
        activity.SetTag(RittenTelemetry.PipelineResult, RittenTelemetry.Result(result.ExitCode));
        if (result.FailedStep is { } failed)
        {
            activity.SetStatus(ActivityStatusCode.Error, $"{failed.Step.Name}: {Describe(failed.Result)}");
        }
    }

    private static Activity? StartStep(Step step)
    {
        var activity = RittenTelemetry.Source.StartActivity(step.Name);
        activity?.SetTag(RittenTelemetry.TaskName, step.Name);
        activity?.SetTag(RittenTelemetry.StepKind, step.Kind.ToString().ToLowerInvariant());
        return activity;
    }

    private static void CompleteStep(Activity? activity, StepResult result)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(RittenTelemetry.TaskResult, RittenTelemetry.Result(result.ExitCode));
        if (result.IsFailure)
        {
            activity.SetStatus(ActivityStatusCode.Error, Describe(result));
        }
        else
        {
            activity.SetTag(RittenTelemetry.StepContinued, result.Continue);
        }
    }

    private static string Describe(StepResult result) =>
        result.Errors is { Count: > 0 } errors ? string.Join("; ", errors.Select(error => error.Message)) : $"Exit code {result.ExitCode}.";

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
