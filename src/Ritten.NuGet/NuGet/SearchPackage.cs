namespace Ritten.NuGet;

/// <summary>
/// One package in <c>dotnet package search --format json</c>: with <c>--exact-match</c>, one entry for each version.
/// </summary>
internal sealed record SearchPackage(string Id, string? Version);
