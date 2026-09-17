using Ritten.Contracts.FileSystem;

namespace Ritten.Git;

/// <summary>
/// Exposes functionality for interacting with a git repository.
/// </summary>
/// <remarks>
/// By default, all operations run on the current git repository for the workflow.
/// Use <see cref="InRepository"/> to create a new client for a different repo.
/// </remarks>
public interface IGit
{
    /// <summary>
    /// Gets a client for the repository at the given directory.
    /// </summary>
    /// <param name="repository">The working tree of the repository.</param>
    IGit InRepository(IDirectory repository);

    /// <summary>
    /// Gets the root of the repository, or <c>null</c> when the client isn't addressing one.
    /// </summary>
    Task<IDirectory?> RepositoryRoot(CancellationToken ct = default);

    /// <summary>
    /// Gets the branch that is checked out, or <c>null</c> when <c>HEAD</c> is detached.
    /// </summary>
    Task<string?> CurrentBranch(CancellationToken ct = default);

    /// <summary>
    /// Gets the upstream of the checked-out branch as <c>remote/branch</c>, or <c>null</c> when it has none.
    /// </summary>
    Task<string?> Upstream(CancellationToken ct = default);

    /// <summary>
    /// Counts the commits on <c>HEAD</c> that the given reference — typically the upstream — doesn't have.
    /// </summary>
    Task<int> CommitsAhead(string reference, CancellationToken ct = default);

    /// <summary>
    /// Gets the URL of the given remote, or <c>null</c> when the remote doesn't exist.
    /// </summary>
    Task<string?> GetRemoteUrl(string remote, CancellationToken ct = default);

    /// <summary>
    /// Adds a remote with the given URL.
    /// </summary>
    Task AddRemote(string remote, string url, CancellationToken ct = default);

    /// <summary>
    /// Reads the given file as it exists at the given reference, or <c>null</c> when it doesn't exist there.
    /// </summary>
    Task<string?> Show(string reference, string path, CancellationToken ct = default);

    /// <summary>
    /// Lists the paths under the given path that differ from the committed state, including untracked files.
    /// </summary>
    Task<IReadOnlyList<string>> ChangedFiles(string path, CancellationToken ct = default);

    /// <summary>
    /// Stages every change under the given path — modifications, additions and deletions alike.
    /// </summary>
    Task Stage(string path, CancellationToken ct = default);

    /// <summary>
    /// Commits what is staged with the given message.
    /// </summary>
    Task Commit(string message, CancellationToken ct = default);

    /// <summary>
    /// Pushes the given branch to the given remote.
    /// </summary>
    /// <param name="remote">The remote to push to.</param>
    /// <param name="branch">The branch to push.</param>
    /// <param name="credential">What to authenticate with, when the remote's own configuration doesn't answer.</param>
    /// <param name="setUpstream">Whether to record the remote branch as the local one's upstream.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Push(
        string remote,
        string branch,
        GitCredential? credential = null,
        bool setUpstream = false,
        CancellationToken ct = default
    );

    /// <summary>
    /// Checks whether the given tag exists in the local repository.
    /// </summary>
    Task<bool> TagExists(string tag, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the given tag exists on the given remote.
    /// </summary>
    Task<bool> RemoteTagExists(string remote, string tag, CancellationToken ct = default);

    /// <summary>
    /// Creates a lightweight tag pointing at the given commit, or at <c>HEAD</c> if no commit is given.
    /// </summary>
    Task CreateTag(string tag, string? commit = null, CancellationToken ct = default);

    /// <summary>
    /// Pushes the given tag to the given remote.
    /// </summary>
    Task PushTag(string remote, string tag, CancellationToken ct = default);
}
