namespace Wolfe.Hamelin.Git;

/// <summary>
/// Exposes functionality for interacting with the git repository the pipeline is running in.
/// </summary>
public interface IGit
{
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
