using Ritten.Engine;

namespace Ritten.Contracts;

/// <summary>
/// An invariant rule about a job that must remain true.
/// </summary>
public interface IJobRule
{
    /// <summary>
    /// Checks the job, returning an error for each violated invariant.
    /// </summary>
    /// <param name="job">The job to judge.</param>
    IEnumerable<Error> Check(IJob job);
}
