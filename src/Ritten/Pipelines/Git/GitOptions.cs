using Ritten.Core;

namespace Ritten.Pipelines.Git;

/// <summary>
/// Settings for the git tagging steps.
/// </summary>
public class GitOptions
{
    /// <summary>
    /// The prefix for release tag names (tags are <c>TagPrefix + version</c>, e.g. <c>v1.2.0</c>).
    /// Also used when validating the changelog's compare links, so tags and links can't drift apart.
    /// </summary>
    public string TagPrefix { get; set; } = "v";

    /// <summary>
    /// The commit to tag; <c>HEAD</c> when not set.
    /// </summary>
    public string? CommitSha { get; set; }

    /// <summary>
    /// Configures the given options based on the current environment.
    /// </summary>
    public static void ConfigureFromEnvironment(GitOptions options) =>
        ConfigureFromEnvironment(options, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Configures the given options from the given environment.
    /// </summary>
    internal static void ConfigureFromEnvironment(GitOptions options, Func<string, string?> envVar) =>
        options.CommitSha = envVar(RittenEnvironment.CommitSha);
}
