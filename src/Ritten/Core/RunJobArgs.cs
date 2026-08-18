using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// What the command line asked for: the job to run, and how to run it.
/// </summary>
/// <param name="Job">The name of the job to run.</param>
public sealed record RunJobArgs(string Job)
{
    /// <summary>
    /// The lowest level of message to print.
    /// </summary>
    public PipelineLogLevel LogLevel { get; init; } = PipelineLogLevel.Detail;

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
}
