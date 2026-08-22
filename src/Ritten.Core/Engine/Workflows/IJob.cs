using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Engine.Workflows;

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
    /// What the job does, as help text.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// What the job is for.
    /// </summary>
    JobKind Kind { get; }

    /// <summary>
    /// The job's steps, in declaration order.
    /// </summary>
    IReadOnlyList<Step> Steps { get; }

    /// <summary>
    /// The values the job takes from whoever invokes it.
    /// </summary>
    IReadOnlyList<JobArgument> Arguments => [];

    /// <summary>
    /// Reads the given project's settings as this job's settings type.
    /// </summary>
    internal Result<WorkflowSettings> ReadSettings(RittenProject project, Func<string, string?> environment, bool dryRun, IWorkflowLog log);

    /// <summary>
    /// Configures the run with the given settings.
    /// </summary>
    internal void Configure(IWorkflowBuilder builder, WorkflowSettings settings, JobArguments args);
}
