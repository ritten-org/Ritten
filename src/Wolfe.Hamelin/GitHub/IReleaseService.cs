namespace Wolfe.Hamelin.GitHub;

/// <summary>
/// Manages GitHub releases for the repository the pipeline is running against.
/// </summary>
public interface IReleaseService
{
    /// <summary>
    /// Checks whether a release exists for the given tag.
    /// </summary>
    Task<bool> Exists(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a release for the given tag.
    /// </summary>
    /// <param name="tag">The git tag the release is for.</param>
    /// <param name="title">The release title.</param>
    /// <param name="notes">The release notes, as markdown.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task Create(string tag, string title, string notes, CancellationToken cancellationToken = default);
}
