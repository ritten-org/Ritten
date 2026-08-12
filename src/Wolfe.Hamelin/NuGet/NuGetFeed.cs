namespace Wolfe.Hamelin.NuGet;

/// <summary>
/// Represents a NuGet package feed, with optional credentials for private feeds.
/// </summary>
/// <param name="Url">The V3 index URL of the feed (e.g. <c>https://api.nuget.org/v3/index.json</c>).</param>
public record NuGetFeed(string Url)
{
    /// <summary>
    /// The username used to authenticate with the feed, for feeds that require one.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// The password or access token used to authenticate with the feed.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Returns a copy of the feed with the given credentials.
    /// </summary>
    public NuGetFeed WithCredentials(string username, string password) => this with { Username = username, Password = password };
}
