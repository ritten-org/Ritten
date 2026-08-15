namespace Ritten.GitHub;

/// <summary>
/// Manages GitHub releases for a repository.
/// </summary>
public interface IReleaseService
{
    /// <summary>
    /// Checks whether a release exists for the given tag.
    /// </summary>
    /// <param name="repository">The repository the release would live in.</param>
    /// <param name="tag">The git tag the release is for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task<bool> Exists(RepositoryPath repository, string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a release for the given tag.
    /// </summary>
    /// <param name="repository">The repository to create the release in.</param>
    /// <param name="tag">The git tag the release is for.</param>
    /// <param name="title">The release title.</param>
    /// <param name="notes">The release notes, as markdown.</param>
    /// <param name="makeLatest">Whether GitHub should mark this release as the repository's latest; backports pass <c>false</c>.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task Create(RepositoryPath repository, string tag, string title, string notes, bool makeLatest = true, CancellationToken cancellationToken = default);
}
