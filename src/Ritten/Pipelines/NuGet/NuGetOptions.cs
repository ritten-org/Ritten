using Ritten.Core;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Settings for the NuGet feed the package is validated against and published to.
/// </summary>
public class NuGetOptions
{
    /// <summary>
    /// The V3 index URL of the feed.
    /// </summary>
    public string Feed { get; set; } = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// The API key used to push packages. Only needed by the deploy pipeline;
    /// <see cref="NugetPush"/> fails with a clear message if it's missing.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Skips the published-version check entirely (e.g. for dependabot pull requests).
    /// </summary>
    public bool SkipVersionCheck { get; set; }

    /// <summary>
    /// Configures the given options based on the current environment.
    /// </summary>
    public static void ConfigureFromEnvironment(NuGetOptions options) =>
        ConfigureFromEnvironment(options, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Configures the given options from the given environment.
    /// </summary>
    internal static void ConfigureFromEnvironment(NuGetOptions options, Func<string, string?> envVar)
    {
        options.ApiKey = envVar(RittenEnvironment.NuGetApiKey);
        options.SkipVersionCheck = bool.TryParse(envVar(RittenEnvironment.SkipVersionCheck), out var skip) && skip;
    }
}
