namespace Wolfe.Hamelin.Pipelines.NuGet;

/// <summary>
/// Settings for the NuGet feed the package is validated against and published to.
/// Bound from the <c>NuGet</c> configuration section.
/// </summary>
public class NuGetOptions
{
    /// <summary>
    /// The V3 index URL of the feed.
    /// </summary>
    public string Feed { get; set; } = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// The API key used to push packages. Only needed by the deploy pipeline;
    /// <see cref="NuGetPush"/> fails with a clear message if it's missing.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Skips the published-version check entirely (e.g. for dependabot pull requests).
    /// </summary>
    public bool SkipVersionCheck { get; set; }
}
