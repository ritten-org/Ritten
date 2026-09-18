using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Git;

/// <summary>
/// Reports changes rather than executing them.
/// </summary>
internal class DryRunGit(IWorkflowLog log, IGit inner) : IGit
{
    /// <inheritdoc />
    public IGit InRepository(IDirectory repository) => new DryRunGit(log, inner.InRepository(repository));

    /// <inheritdoc />
    public Task<bool> IsRepository(CancellationToken ct = default) => inner.IsRepository(ct);

    /// <inheritdoc />
    public Task<IDirectory?> RepositoryRoot(CancellationToken ct = default) =>
        inner.RepositoryRoot(ct);

    /// <inheritdoc />
    public Task<string?> CurrentBranch(CancellationToken ct = default) =>
        inner.CurrentBranch(ct);

    /// <inheritdoc />
    public Task<string?> Upstream(CancellationToken ct = default) =>
        inner.Upstream(ct);

    /// <inheritdoc />
    public Task<int> CommitsAhead(string reference, CancellationToken ct = default) =>
        inner.CommitsAhead(reference, ct);

    /// <inheritdoc />
    public Task<string?> GetRemoteUrl(string remote, CancellationToken ct = default) =>
        inner.GetRemoteUrl(remote, ct);

    /// <inheritdoc />
    public Task AddRemote(string remote, string url, CancellationToken ct = default)
    {
        log.Skipped($"Would add remote {remote} at {url}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> Show(string reference, string path, CancellationToken ct = default) =>
        inner.Show(reference, path, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ChangedFiles(string path, CancellationToken ct = default) =>
        inner.ChangedFiles(path, ct);

    /// <inheritdoc />
    public Task Stage(string path, CancellationToken ct = default)
    {
        log.Skipped($"Would stage the changes under {path}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Commit(string message, CancellationToken ct = default)
    {
        log.Skipped($"Would commit \"{message}\".");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Push(
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
    public Task<bool> TagExists(string tag, CancellationToken ct = default) =>
        inner.TagExists(tag, ct);

    /// <inheritdoc />
    public Task<bool> RemoteTagExists(string remote, string tag, CancellationToken ct = default) =>
        inner.RemoteTagExists(remote, tag, ct);

    /// <inheritdoc />
    public Task CreateTag(string tag, string? commit = null, CancellationToken ct = default)
    {
        log.Skipped($"Would create tag {tag}{(commit is null ? "" : $" at {commit}")}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PushTag(string remote, string tag, CancellationToken ct = default)
    {
        log.Skipped($"Would push tag {tag} to {remote}.");
        return Task.CompletedTask;
    }
}
