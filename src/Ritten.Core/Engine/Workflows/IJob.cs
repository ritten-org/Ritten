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
    /// The type of the values the job runs with.
    /// </summary>
    Type ArgumentsType { get; }

    /// <summary>
    /// The types the job's arguments always put into the run's state, ahead of its first step.
    /// </summary>
    IReadOnlyCollection<Type> Inputs => [.. ArgumentsModel.For(ArgumentsType).Inputs.Where(i => !i.Optional).Select(i => i.Type)];

    /// <summary>
    /// The values the job takes from the command line rather than the project file.
    /// </summary>
    IReadOnlyList<JobOption> Options => ArgumentsModel.For(ArgumentsType).Options;

    /// <summary>
    /// Whether the job needs a project file to run from the command line.
    /// </summary>
    bool RequiresProject => true;

    /// <summary>
    /// Whether the run publishes a build report, when the application reports at all.
    /// </summary>
    bool Reports => true;

    /// <summary>
    /// Judges the arguments the job is about to run with.
    /// </summary>
    internal IReadOnlyList<Error> Validate(object arguments, Func<string, string?> environment, bool dryRun, IWorkflowLog log, string source);
}
