using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// How to run a job, whichever job it is.
/// </summary>
public sealed record RunOptions
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
}
