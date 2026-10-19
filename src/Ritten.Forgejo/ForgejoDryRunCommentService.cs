using Ritten.Reporting;

namespace Ritten.Forgejo;

/// <summary>
/// Reports that a comment would be posted or removed instead of doing it, and reads through the real service.
/// </summary>
internal sealed class ForgejoDryRunCommentService(IForgejoCommentService inner, IWorkflowLog log) : IForgejoCommentService
{
    /// <inheritdoc />
    public string? Unavailable => inner.Unavailable;

    /// <inheritdoc />
    public Task<IReadOnlyList<ReportKey>> List(CancellationToken cancellationToken = default) => inner.List(cancellationToken);

    /// <inheritdoc />
    public Task CreateOrUpdate(ReportKey key, string body, CancellationToken cancellationToken = default)
    {
        log.Skipped("Would post the build report as a pull request comment.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Delete(ReportKey key, CancellationToken cancellationToken = default)
    {
        log.Skipped("Would remove the pull request comment.");
        return Task.CompletedTask;
    }
}
