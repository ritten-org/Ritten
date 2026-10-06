using System.Diagnostics;
using Ritten.Contracts;

namespace Ritten.Reporting;

/// <summary>
/// Telemetry constants for Ritten.
/// </summary>
public static class RittenTelemetry
{
    /// <summary>
    /// The name of the activity source every span of a run comes from, for a listener or a tracer provider to
    /// subscribe to.
    /// </summary>
    public const string SourceName = "Ritten";

    internal static readonly ActivitySource Source = new(SourceName, typeof(RittenTelemetry).Assembly.GetName().Version?.ToString());

    // The run, as the CI system calls it: present only when a runtime knows it.
    internal const string PipelineName = "cicd.pipeline.name";
    internal const string PipelineRunId = "cicd.pipeline.run.id";
    internal const string PipelineRunUrl = "cicd.pipeline.run.url.full";
    internal const string PipelineResult = "cicd.pipeline.result";
    internal const string TaskName = "cicd.pipeline.task.name";
    internal const string TaskResult = "cicd.pipeline.task.run.result";

    internal const string Workflow = "ritten.workflow";
    internal const string Job = "ritten.job";
    internal const string DryRun = "ritten.dry_run";
    internal const string RunExitCode = "ritten.exit_code";
    internal const string StepKind = "ritten.step.kind";

    // Whether a step that succeeded let the job go on: one that didn't finished it early.
    internal const string StepContinued = "ritten.step.continued";

    internal const string Executable = "process.executable.name";
    internal const string ProcessExitCode = "process.exit.code";

    /// <summary>
    /// What became of a run or a step, in the conventions' words.
    /// </summary>
    internal static string Result(ExitCode exitCode) =>
        exitCode == ExitCode.Cancelled ? "cancellation" : exitCode.IsSuccess ? "success" : "failure";
}
