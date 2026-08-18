using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.GitHub;

/// <summary>
/// Reports that a comment would be posted instead of posting it. Nothing on this interface reads,
/// so the real service is never needed.
/// </summary>
internal class DryRunCommentService(IWorkflowLog log) : ICommentService
{
    /// <inheritdoc />
    public Task CreateOrUpdate(string body, CancellationToken cancellationToken = default)
    {
        log.Skipped("Would post the build report as a pull request comment.");
        return Task.CompletedTask;
    }
}
