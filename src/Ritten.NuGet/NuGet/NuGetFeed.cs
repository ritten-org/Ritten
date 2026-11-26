namespace Ritten.NuGet;

/// <summary>
/// Represents a NuGet package feed, with the API key to push to it.
/// </summary>
/// <param name="Url">The V3 index URL of the feed (e.g. <c>https://api.nuget.org/v3/index.json</c>).</param>
public record NuGetFeed(string Url)
{
    /// <summary>
    /// The API key used to push packages to the feed.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Returns a copy of the feed with the given push API key.
    /// </summary>
    public NuGetFeed WithApiKey(string apiKey) => this with { ApiKey = apiKey };
}
