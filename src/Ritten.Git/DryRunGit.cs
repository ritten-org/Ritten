using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Git;

/// <summary>
/// Reports changes rather than executing them.
/// </summary>
internal class DryRunGit(IWorkflowLog log, IGit inner) : IGit
{
    /// <inheritdoc />
    public Task<bool> IsRepository(IDirectory repository, CancellationToken ct = default) => inner.IsRepository(repository, ct);

    /// <inheritdoc />
    public Task<IDirectory?> RepositoryRoot(IDirectory repository, CancellationToken ct = default) =>
        inner.RepositoryRoot(repository, ct);

    /// <inheritdoc />
    public Task<string?> CurrentBranch(IDirectory repository, CancellationToken ct = default) =>
        inner.CurrentBranch(repository, ct);

    /// <inheritdoc />
    public Task<string?> Upstream(IDirectory repository, CancellationToken ct = default) =>
        inner.Upstream(repository, ct);

    /// <inheritdoc />
    public Task<int> CommitsAhead(IDirectory repository, string reference, CancellationToken ct = default) =>
        inner.CommitsAhead(repository, reference, ct);

    /// <inheritdoc />
    public Task<string?> GetRemoteUrl(IDirectory repository, string remote, CancellationToken ct = default) =>
        inner.GetRemoteUrl(repository, remote, ct);

    /// <inheritdoc />
    public Task AddRemote(IDirectory repository, string remote, string url, CancellationToken ct = default)
    {
        log.Skipped($"Would add remote {remote} at {url}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> Show(IDirectory repository, string reference, string path, CancellationToken ct = default) =>
        inner.Show(repository, reference, path, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ChangedFiles(IDirectory repository, string path, CancellationToken ct = default) =>
        inner.ChangedFiles(repository, path, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ChangedFilesSince(IDirectory repository, string reference, string path, CancellationToken ct = default) =>
        inner.ChangedFilesSince(repository, reference, path, ct);

    /// <inheritdoc />
    public Task FetchMergeBase(IDirectory repository, string remote, string branch, CancellationToken ct = default) =>
        inner.FetchMergeBase(repository, remote, branch, ct);

    /// <inheritdoc />
    public Task Stage(IDirectory repository, string path, CancellationToken ct = default)
    {
        log.Skipped($"Would stage the changes under {path}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Commit(IDirectory repository, string message, CancellationToken ct = default)
    {
        log.Skipped($"Would commit \"{message}\".");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Push(
        IDirectory repository,
        string remote,
        string branch,
        GitCredential? credential = null,
        bool setUpstream = false,
        CancellationToken ct = default)
    {
        log.Skipped($"Would push {branch} to {remote}{(credential is null ? "" : $" as {credential.Username}")}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> TagExists(IDirectory repository, string tag, CancellationToken ct = default) =>
        inner.TagExists(repository, tag, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> Tags(IDirectory repository, string pattern = "*", CancellationToken ct = default) =>
        inner.Tags(repository, pattern, ct);

    /// <inheritdoc />
    public Task<bool> IsShallow(IDirectory repository, CancellationToken ct = default) => inner.IsShallow(repository, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> TrackedFiles(IDirectory repository, IReadOnlyList<string>? pathspecs = null, CancellationToken ct = default) =>
        inner.TrackedFiles(repository, pathspecs, ct);

    /// <inheritdoc />
    public Task<bool> RemoteTagExists(IDirectory repository, string remote, string tag, CancellationToken ct = default) =>
        inner.RemoteTagExists(repository, remote, tag, ct);

    /// <inheritdoc />
    public Task CreateTag(IDirectory repository, string tag, string? commit = null, CancellationToken ct = default)
    {
        log.Skipped($"Would create tag {tag}{(commit is null ? "" : $" at {commit}")}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PushTag(IDirectory repository, string remote, string tag, CancellationToken ct = default)
    {
        log.Skipped($"Would push tag {tag} to {remote}.");
        return Task.CompletedTask;
    }
}
