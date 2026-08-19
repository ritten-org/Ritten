namespace Ritten.Git;

/// <summary>
/// Exposes functionality for interacting with the git repository the workflow is running in.
/// </summary>
public interface IGit
{
    /// <summary>
    /// Gets the URL of the given remote, or <c>null</c> when the remote doesn't exist.
    /// </summary>
    Task<string?> GetRemoteUrl(string remote, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the given file as it exists at the given reference, or <c>null</c> when it doesn't exist there.
    /// </summary>
    Task<string?> Show(string reference, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the paths under the given path that differ from the committed state, including untracked files.
    /// </summary>
    Task<IReadOnlyList<string>> ChangedFiles(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the given tag exists in the local repository.
    /// </summary>
    Task<bool> TagExists(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the given tag exists on the given remote.
    /// </summary>
    Task<bool> RemoteTagExists(string remote, string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a lightweight tag pointing at the given commit, or at <c>HEAD</c> if no commit is given.
    /// </summary>
    Task CreateTag(string tag, string? commit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes the given tag to the given remote.
    /// </summary>
    Task PushTag(string remote, string tag, CancellationToken cancellationToken = default);
}
