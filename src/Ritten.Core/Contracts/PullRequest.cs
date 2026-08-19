using System.Diagnostics.CodeAnalysis;

namespace Ritten.Contracts;

/// <summary>
/// What the active runtime knows about the pull request under review.
/// </summary>
public sealed record PullRequest
{
    /// <summary>
    /// The number of the pull request, or null when the run isn't reviewing one.
    /// </summary>
    public int? Number { get; init; }

    /// <summary>
    /// The ref the pull request wants to merge into, or null when the runtime doesn't know.
    /// </summary>
    public string? BaseRef { get; init; }

    /// <summary>
    /// Whether this run is reviewing a pull request at all.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Number))]
    public bool IsPullRequest => Number is not null;
}
