using System.CommandLine;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.CommandLine;

/// <summary>
/// The flags the engine understands, which are true of every job.
/// </summary>
internal sealed class WorkflowFlags
{
    public Option<bool> Verbose { get; } = new($"--{WorkflowArguments.Verbose}", "-v")
    {
        Description = "Show every log entry in its highest detail.",
        Recursive = true
    };

    public Option<bool> Quiet { get; } = new($"--{WorkflowArguments.Quiet}", "-q")
    {
        Description = "Show each step's outcome, but only failure detail.",
        Recursive = true
    };

    public Option<bool> DryRun { get; } = new($"--{WorkflowArguments.DryRun}")
    {
        Description = "Rehearse the job without pushing, tagging, releasing, or commenting.",
        Recursive = true
    };

    public Option<bool> AutoApprove { get; } = new($"--{WorkflowArguments.AutoApprove}")
    {
        Description = "Approve a job up front, for runs with nobody there to confirm.",
        Recursive = true
    };

    public IEnumerable<Option> Options => [Verbose, Quiet, DryRun, AutoApprove];

    public WorkflowLogLevel LogLevel(ParseResult parseResult) => parseResult.GetValue(Verbose)
        ? WorkflowLogLevel.Verbose
        : parseResult.GetValue(Quiet)
            ? WorkflowLogLevel.Warning
            : WorkflowLogLevel.Detail;
}
