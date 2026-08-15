using Ritten.Core;

namespace Ritten.Contracts;

/// <summary>
/// An invariant rule about a job that must remain true.
/// </summary>
public interface IJobRule
{
    /// <summary>
    /// Checks the job's steps, returning an error for each violated invariant.
    /// </summary>
    /// <param name="steps">The job's steps, in the order they would run.</param>
    IEnumerable<Error> Check(IReadOnlyList<JobStep> steps);
}
