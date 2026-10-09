using Ritten.NuGet.Steps;

namespace Ritten.NuGet;

/// <summary>
/// Settings for publishing to a NuGet feed, read from the environment.
/// </summary>
public class NuGetOptions
{
    /// <summary>
    /// The environment variable the push API key is read from.
    /// </summary>
    public const string ApiKeyVariable = "RITTEN_NUGET_API_KEY";

    /// <summary>
    /// The API key used to push packages. Only needed when deploying;
    /// <see cref="NugetAuthenticate"/> asks at the terminal when it's missing.
    /// </summary>
    public string? ApiKey { get; set; }

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
