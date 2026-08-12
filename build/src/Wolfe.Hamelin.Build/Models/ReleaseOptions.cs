namespace Wolfe.Hamelin.Build.Models;

// Bound from the "Release" section.
public class ReleaseOptions
{
    /// <summary>
    /// Tag names are TagPrefix + version, e.g. v1.2.0. Must match the changelog's compare links.
    /// </summary>
    public string TagPrefix { get; set; } = "v";

    /// <summary>
    /// The commit to tag; HEAD when not set.
    /// </summary>
    public string? CommitSha { get; set; }
}
