namespace Ritten.NuGet;

/// <summary>
/// One source's packages in <c>dotnet package search --format json</c>.
/// </summary>
internal sealed record SearchSource(string? SourceName, IReadOnlyList<SearchPackage> Packages);
