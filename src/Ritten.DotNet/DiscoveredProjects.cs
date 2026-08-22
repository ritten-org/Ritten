namespace Ritten.DotNet;

/// <summary>
/// The C# projects found in the repository.
/// </summary>
/// <param name="Shipped">The projects that aren't tests, in path order, relative to the project root.</param>
/// <param name="Tests">The test projects, in path order, relative to the project root.</param>
public sealed record DiscoveredProjects(IReadOnlyList<string> Shipped, IReadOnlyList<string> Tests);
