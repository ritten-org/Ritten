namespace Ritten.GitHub;

/// <summary>
/// Options for the client the workflow talks to GitHub itself with.
/// </summary>
public class GitHubClientOptions
{
    /// <summary>
    /// The product name used to identify the workflow to the GitHub API.
    /// </summary>
    public string ClientName { get; set; } = "Ritten";

    /// <summary>
    /// The token used to authenticate with the GitHub API: an explicit <c>GH_TOKEN</c> first,
    /// falling back to whatever ambient credential the active runtime offers.
    /// </summary>
    public string? Token { get; set; }
}
