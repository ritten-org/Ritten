namespace Ritten.Git;

/// <summary>
/// Settings for the git tagging steps, read from the environment.
/// </summary>
public class GitOptions
{
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
    private static void ConfigureFromEnvironment(GitOptions options, Func<string, string?> envVar) =>
        options.CommitSha = envVar(GitEnvironment.CommitSha);
}
