using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// What the command line asked for: the job to run, and how to run it.
/// </summary>
/// <param name="Job">The name of the job to run.</param>
public sealed record RunJobArgs(string Job)
{
    /// <summary>
    /// The lowest level of message to print.
    /// </summary>
    public WorkflowLogLevel LogLevel { get; init; } = WorkflowLogLevel.Detail;

    /// <summary>
    /// The directory on the file system in which to run the job.
    /// </summary>
    public string Directory { get; init; } = Environment.CurrentDirectory;

    /// <summary>
    /// Rehearses the job without doing anything that reaches outside the working directory.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// Approves a job that would otherwise stop and ask.
    /// </summary>
    public bool AutoApprove { get; init; }

    /// <summary>
    /// The values supplied for the inputs the job declares, keyed by input name.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Arguments { get; init; } = new Dictionary<string, string?>();
}
