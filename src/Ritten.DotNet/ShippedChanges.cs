namespace Ritten.DotNet;

/// <summary>
/// What a pull request changed of the files that decide what the packages contain.
/// </summary>
/// <param name="BaseRef">The ref the changes are measured against, or null when the run isn't reviewing a pull request.</param>
/// <param name="Files">The changed files, relative to the repository root; empty when nothing shipped changed.</param>
public sealed record ShippedChanges(string? BaseRef, IReadOnlyList<string> Files)
{
    /// <summary>
    /// Nothing to compare against: the run isn't reviewing a pull request.
    /// </summary>
    public static ShippedChanges Unreviewed { get; } = new(null, []);

    /// <summary>
    /// Whether the changes were measured against a pull request's base at all.
    /// </summary>
    public bool Reviewed => BaseRef is not null;

    /// <summary>
    /// Whether anything that ships changed.
    /// </summary>
    public bool Any => Files.Count > 0;
}
