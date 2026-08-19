namespace Ritten.DotNet;

/// <summary>
/// Every package the repository ships, as read from their project files.
/// </summary>
public sealed record PackageSet
{
    /// <summary>
    /// The packages, in the order the project declares them.
    /// </summary>
    public required IReadOnlyList<Project> Packages { get; init; }
}
