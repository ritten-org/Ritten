using Ritten.Core;

namespace Ritten.Pipelines;

/// <summary>
/// Settings for changelog validation.
/// </summary>
public class ChangelogOptions
{
    /// <summary>
    /// The changelog file, relative to the repository root.
    /// </summary>
    public string File { get; set; } = "CHANGELOG.md";

    /// <summary>
    /// The repository's web URL; when set, the changelog's version links are validated against it.
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Skips changelog validation entirely (e.g. for dependabot pull requests).
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Configures the given options based on the current environment.
    /// </summary>
    public static void ConfigureFromEnvironment(ChangelogOptions options) =>
        ConfigureFromEnvironment(options, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Configures the given options from the given environment.
    /// </summary>
    internal static void ConfigureFromEnvironment(ChangelogOptions options, Func<string, string?> envVar) =>
        options.Skip = bool.TryParse(envVar(RittenEnvironment.SkipChangelog), out var skip) && skip;
}
