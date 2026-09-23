using Ritten.NuGet.Steps;
using Ritten.Releases;

namespace Ritten.NuGet;

/// <summary>
/// Settings for the NuGet feed the package is checked against and published to.
/// </summary>
public class NuGetOptions
{
    /// <summary>
    /// The environment variable the push API key is read from.
    /// </summary>
    public const string ApiKeyVariable = "RITTEN_NUGET_API_KEY";

    /// <summary>
    /// The V3 index URL of the feed.
    /// </summary>
    public string Feed { get; set; } = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// The API key used to push packages. Only needed when deploying;
    /// <see cref="NugetAuthenticate"/> asks at the terminal when it's missing.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// How published versions are grouped into release lines when validating the project's version.
    /// </summary>
    public ReleaseLine Lines { get; set; } = ReleaseLine.Major;

    /// <summary>
    /// When a merged change becomes a release.
    /// </summary>
    public ReleaseCadence Cadence { get; set; } = ReleaseCadence.Curated;

    /// <summary>
    /// Configures the given options based on the current environment.
    /// </summary>
    public static void ConfigureFromEnvironment(NuGetOptions options) =>
        ConfigureFromEnvironment(options, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Configures the given options from the given environment.
    /// </summary>
    internal static void ConfigureFromEnvironment(NuGetOptions options, Func<string, string?> envVar) =>
        options.ApiKey = envVar(ApiKeyVariable);
}
