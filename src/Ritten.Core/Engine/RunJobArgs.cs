using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// What the command line asked of a job.
/// </summary>
/// <param name="Job">The name of the job to run.</param>
public sealed record RunJobArgs(string Job)
{
    /// <summary>
    /// The lowest level of message to print.
    /// </summary>
    public WorkflowLogLevel LogLevel { get; init; } = WorkflowLogLevel.Detail;

    /// <summary>
    /// Rehearses the job without doing anything that reaches outside the working directory.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Approves a job that would otherwise stop and ask.
    /// </summary>
    public bool AutoApprove { get; init; }

    /// <summary>
    /// The arguments supplied to the job.
    /// </summary>
    public JobArguments Arguments { get; init; } = JobArguments.None;
}
