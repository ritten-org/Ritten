namespace Ritten.Core;

/// <summary>
/// The environment variables Ritten defines.
/// </summary>
internal static class RittenEnvironment
{
    /// <summary>
    /// The API key used to push packages.
    /// </summary>
    public const string NuGetApiKey = "RITTEN_NUGET_API_KEY";

    /// <summary>
    /// Skips the published-version check.
    /// </summary>
    public const string SkipVersionCheck = "RITTEN_SKIP_VERSION_CHECK";

    /// <summary>
    /// Skips changelog validation.
    /// </summary>
    public const string SkipChangelog = "RITTEN_SKIP_CHANGELOG";

    /// <summary>
    /// The commit to tag.
    /// </summary>
    public const string CommitSha = "RITTEN_COMMIT_SHA";
}
