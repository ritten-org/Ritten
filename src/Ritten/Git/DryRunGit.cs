using Ritten.Reporting;

namespace Ritten.Git;

/// <summary>
/// Reports what would be tagged instead of tagging it.
/// </summary>
internal class DryRunGit(IWorkflowLog log, IGit inner) : IGit
{
    /// <inheritdoc />
    public Task<string?> GetRemoteUrl(string remote, CancellationToken cancellationToken = default) =>
        inner.GetRemoteUrl(remote, cancellationToken);

    /// <inheritdoc />
    public Task<bool> TagExists(string tag, CancellationToken cancellationToken = default) =>
        inner.TagExists(tag, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RemoteTagExists(string remote, string tag, CancellationToken cancellationToken = default) =>
        inner.RemoteTagExists(remote, tag, cancellationToken);

    /// <inheritdoc />
    public Task CreateTag(string tag, string? commit = null, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would create tag {tag}{(commit is null ? "" : $" at {commit}")}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PushTag(string remote, string tag, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would push tag {tag} to {remote}.");
        return Task.CompletedTask;
    }
}
