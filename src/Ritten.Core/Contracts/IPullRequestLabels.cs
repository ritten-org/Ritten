namespace Ritten.Contracts;

/// <summary>
/// Reads the labels on the pull request under review.
/// </summary>
public interface IPullRequestLabels
{
    /// <summary>
    /// Gets the labels currently on the pull request, or null if the capability is not available.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<IReadOnlyList<Label>?> Get(CancellationToken cancellationToken = default);
}
