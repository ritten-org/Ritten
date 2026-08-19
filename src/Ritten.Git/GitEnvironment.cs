namespace Ritten.Git;

/// <summary>
/// The environment variables the git module reads.
/// </summary>
internal static class GitEnvironment
{
    /// <summary>
    /// The commit to tag, for CI workflows where the checked-out commit isn't the one being released.
    /// </summary>
    public const string CommitSha = "RITTEN_COMMIT_SHA";
}
