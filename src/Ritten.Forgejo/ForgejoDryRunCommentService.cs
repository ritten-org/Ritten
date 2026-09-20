using Ritten.Reporting;

namespace Ritten.Forgejo;

/// <summary>
/// Reports that a comment would be posted instead of posting it. Nothing on this interface reads,
/// so the real service is never needed.
/// </summary>
internal sealed class ForgejoDryRunCommentService(IWorkflowLog log) : IForgejoCommentService
{
    /// <inheritdoc />
    public Task CreateOrUpdate(string body, CancellationToken cancellationToken = default)
    {
        log.Skipped("Would post the build report as a pull request comment.");
        return Task.CompletedTask;
    }
}
