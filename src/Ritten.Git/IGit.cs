using Ritten.Contracts.FileSystem;

namespace Ritten.Git;

/// <summary>
/// Exposes functionality for interacting with a git repository.
/// </summary>
/// <remarks>
/// Holds no repository of its own: every operation names the one it acts on, so one client serves every repository
/// a host works with.
/// </remarks>
public interface IGit
{
    /// <summary>
    /// Checks whether the directory is a git working tree, or inside one.
    /// </summary>
    Task<bool> IsRepository(IDirectory repository, CancellationToken ct = default);

    /// <summary>
    /// Gets the root of the repository, or <c>null</c> when the directory is in none.
    /// </summary>
    Task<IDirectory?> RepositoryRoot(IDirectory repository, CancellationToken ct = default);

    /// <summary>
    /// Gets the branch that is checked out, or <c>null</c> when <c>HEAD</c> is detached.
    /// </summary>
    Task<string?> CurrentBranch(IDirectory repository, CancellationToken ct = default);

    /// <summary>
    /// Gets the upstream of the checked-out branch as <c>remote/branch</c>, or <c>null</c> when it has none.
    /// </summary>
    Task<string?> Upstream(IDirectory repository, CancellationToken ct = default);

    /// <summary>
    /// Counts the commits on <c>HEAD</c> that the given reference — typically the upstream — doesn't have.
    /// </summary>
    Task<int> CommitsAhead(IDirectory repository, string reference, CancellationToken ct = default);

    /// <summary>
    /// Gets the URL of the given remote, or <c>null</c> when the remote doesn't exist.
    /// </summary>
    Task<string?> GetRemoteUrl(IDirectory repository, string remote, CancellationToken ct = default);

    /// <summary>
    /// Adds a remote with the given URL.
    /// </summary>
    Task AddRemote(IDirectory repository, string remote, string url, CancellationToken ct = default);

    /// <summary>
    /// Reads the given file as it exists at the given reference, or <c>null</c> when it doesn't exist there.
    /// </summary>
    Task<string?> Show(IDirectory repository, string reference, string path, CancellationToken ct = default);

    /// <summary>
    /// Lists the paths under the given path that differ from the committed state, including untracked files.
    /// </summary>
    Task<IReadOnlyList<string>> ChangedFiles(IDirectory repository, string path, CancellationToken ct = default);

    /// <summary>
    /// Lists the paths under the given path that this branch has changed since it diverged from
    /// the given reference.
    /// </summary>
    /// <param name="repository">The working tree of the repository, or a directory inside it.</param>
    /// <remarks>
    /// If you call this in a PR, make sure to call <see cref="FetchMergeBase"/> first.
    /// </remarks>
    /// <param name="reference">The reference the branch diverged from, such as <c>origin/main</c>.</param>
    /// <param name="path">The path to limit the answer to.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<IReadOnlyList<string>> ChangedFilesSince(IDirectory repository, string reference, string path, CancellationToken ct = default);

    /// <summary>
    /// Fetches the given branch into its remote-tracking reference, with enough history that it
    /// and <c>HEAD</c> share a merge base.
    /// </summary>
    /// <param name="repository">The working tree of the repository, or a directory inside it.</param>
    /// <remarks>
    /// If you call <see cref="ChangedFilesSince"/> from CI, you should call this first..
    /// </remarks>
    /// <param name="remote">The remote to fetch from, such as <c>origin</c>.</param>
    /// <param name="branch">The branch to fetch, such as <c>main</c>.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task FetchMergeBase(IDirectory repository, string remote, string branch, CancellationToken ct = default);

    /// <summary>
    /// Stages every change under the given path — modifications, additions and deletions alike.
    /// </summary>
    Task Stage(IDirectory repository, string path, CancellationToken ct = default);

    /// <summary>
    /// Commits what is staged with the given message.
    /// </summary>
    Task Commit(IDirectory repository, string message, CancellationToken ct = default);

    /// <summary>
    /// Pushes the given branch to the given remote.
    /// </summary>
    /// <param name="repository">The working tree of the repository, or a directory inside it.</param>
    /// <param name="remote">The remote to push to.</param>
    /// <param name="branch">The branch to push.</param>
    /// <param name="credential">What to authenticate with, when the remote's own configuration doesn't answer.</param>
    /// <param name="setUpstream">Whether to record the remote branch as the local one's upstream.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Push(
        IDirectory repository,
        string remote,
        string branch,
        GitCredential? credential = null,
        bool setUpstream = false,
        CancellationToken ct = default
    );

    /// <summary>
    /// Checks whether the given tag exists in the local repository.
    /// </summary>
    Task<bool> TagExists(IDirectory repository, string tag, CancellationToken ct = default);

    /// <summary>
    /// Gets the local tags that match <paramref name="pattern"/>.
    /// </summary>
    Task<IReadOnlyList<string>> Tags(IDirectory repository, string pattern = "*", CancellationToken ct = default);

    /// <summary>
    /// Checks whether the clone is shallow (only holds part of its history).
    /// </summary>
    Task<bool> IsShallow(IDirectory repository, CancellationToken ct = default);

    /// <summary>
    /// Gets the tracked files matching the given pathspecs.
    /// </summary>
    /// <param name="repository">The working tree of the repository, or a directory inside it.</param>
    /// <param name="pathspecs">The path specs to match, or null for all files..</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<IReadOnlyList<string>> TrackedFiles(IDirectory repository, IReadOnlyList<string>? pathspecs = null, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the given tag exists on the given remote.
    /// </summary>
    Task<bool> RemoteTagExists(IDirectory repository, string remote, string tag, CancellationToken ct = default);

    /// <summary>
    /// Creates a lightweight tag pointing at the given commit, or at <c>HEAD</c> if no commit is given.
    /// </summary>
    Task CreateTag(IDirectory repository, string tag, string? commit = null, CancellationToken ct = default);

    /// <summary>
    /// Pushes the given tag to the given remote.
    /// </summary>
    Task PushTag(IDirectory repository, string remote, string tag, CancellationToken ct = default);
}
