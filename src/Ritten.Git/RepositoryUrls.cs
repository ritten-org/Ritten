namespace Ritten.Git;

/// <summary>
/// Normalises the ways a repository gets written down.
/// </summary>
public static class RepositoryUrls
{
    /// <summary>
    /// Converts a repository or remote URL to its web form, or <c>null</c> when there is nothing usable to convert.
    /// </summary>
    /// <param name="url">The URL as configured or reported.</param>
    public static string? ToWebUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var web = url.Trim();
        if (web.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase))
        {
            web = web["ssh://".Length..];
        }

        // scp-style: git@github.com:owner/repo.git
        if (web.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
        {
            web = "https://" + web["git@".Length..].Replace(':', '/');
        }

        if (!web.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !web.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        web = web.TrimEnd('/');
        if (web.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            web = web[..^".git".Length];
        }

        return web;
    }
}
