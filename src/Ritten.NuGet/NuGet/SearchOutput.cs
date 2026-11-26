using System.Text.Json;

namespace Ritten.NuGet;

/// <summary>
/// What <c>dotnet package search --format json</c> writes: each source's packages, and what it could not read.
/// </summary>
internal sealed record SearchOutput(IReadOnlyList<SearchSource> SearchResult, IReadOnlyList<JsonElement>? Problems);
