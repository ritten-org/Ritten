using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// A declared workflow job.
/// </summary>
public interface IJob
{
    /// <summary>
    /// The job's name, as given on the command line.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The job's steps, in declaration order.
    /// </summary>
    IReadOnlyList<Step> Steps { get; }

    /// <summary>
    /// Reads the given project's settings as this job's settings type.
    /// </summary>
    internal Result<WorkflowSettings> ReadSettings(RittenProject project, Func<string, string?> environment, bool dryRun, IWorkflowLog log);

    /// <summary>
    /// Configures the run with the given settings.
    /// </summary>
    internal void Configure(IWorkflowBuilder builder, WorkflowSettings settings);
}
