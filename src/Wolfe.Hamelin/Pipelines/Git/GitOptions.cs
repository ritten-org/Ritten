namespace Wolfe.Hamelin.Pipelines.Git;

/// <summary>
/// Settings for the git tagging steps. Bound from the <c>Git</c> configuration section.
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
}
