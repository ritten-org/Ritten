namespace Ritten;

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
    /// The commit to tag.
    /// </summary>
    public const string CommitSha = "RITTEN_COMMIT_SHA";
}
