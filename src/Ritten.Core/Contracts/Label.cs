namespace Ritten.Contracts;

/// <summary>
/// A label on the pull request under review.
/// </summary>
/// <param name="Name">The label's name, as it appears on the pull request.</param>
public sealed record Label(string Name)
{
    /// <summary>
    /// The label's display color, in the forge's own notation, or null when it has none.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// The label's description, or null when it has none.
    /// </summary>
    public string? Description { get; init; }
}
