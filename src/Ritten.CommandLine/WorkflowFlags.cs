using System.CommandLine;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.CommandLine;

/// <summary>
/// The flags the engine understands, which are true of every job.
/// </summary>
public sealed class WorkflowFlags
{
    /// <summary>
    /// Prints every log entry in its highest detail.
    /// </summary>
    public Option<bool> Verbose { get; } = new($"--{WorkflowArguments.Verbose}", "-v")
    {
        Description = "Show every log entry in its highest detail.",
        Recursive = true
    };

    /// <summary>
    /// Prints each step's outcome, but only failure detail.
    /// </summary>
    public Option<bool> Quiet { get; } = new($"--{WorkflowArguments.Quiet}", "-q")
    {
        Description = "Show each step's outcome, but only failure detail.",
        Recursive = true
    };

    /// <summary>
    /// Rehearses the job.
    /// </summary>
    public Option<bool> DryRun { get; } = new($"--{WorkflowArguments.DryRun}")
    {
        Description = "Rehearse the job without pushing, tagging, releasing, or commenting.",
        Recursive = true
    };

    /// <summary>
    /// Approves the job up front.
    /// </summary>
    public Option<bool> AutoApprove { get; } = new($"--{WorkflowArguments.AutoApprove}")
    {
        Description = "Approve a job up front, for runs with nobody there to confirm.",
        Recursive = true
    };

    /// <summary>
    /// Every flag, to add to a command.
    /// </summary>
    public IEnumerable<Option> Options => [Verbose, Quiet, DryRun, AutoApprove];

    /// <summary>
    /// The lowest level of message the parsed flags ask to print.
    /// </summary>
    /// <param name="parseResult">The parsed command line.</param>
    public WorkflowLogLevel LogLevel(ParseResult parseResult) => parseResult.GetValue(Verbose)
        ? WorkflowLogLevel.Verbose
        : parseResult.GetValue(Quiet)
            ? WorkflowLogLevel.Warning
            : WorkflowLogLevel.Detail;

    /// <summary>
    /// How the parsed flags ask a job to run.
    /// </summary>
    /// <param name="parseResult">The parsed command line.</param>
    public RunOptions RunOptions(ParseResult parseResult) => new()
    {
        LogLevel = LogLevel(parseResult),
        DryRun = parseResult.GetValue(DryRun),
        AutoApprove = parseResult.GetValue(AutoApprove)
    };
}
