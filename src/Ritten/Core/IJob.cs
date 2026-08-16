using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// A declared job: what a pipeline calls it and the steps it runs, in order.
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
}
