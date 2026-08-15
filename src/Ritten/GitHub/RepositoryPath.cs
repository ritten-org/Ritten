namespace Ritten.GitHub;

/// <summary>
/// A repository's owner and name, as GitHub's API addresses it.
/// </summary>
/// <param name="Owner">The user or organisation that owns the repository.</param>
/// <param name="Name">The repository's name.</param>
public sealed record RepositoryPath(string Owner, string Name)
{
    /// <summary>
    /// Parses the owner and name out of a repository's web URL, or returns null when the URL
    /// isn't one (explicit settings arrive unnormalised, so a trailing <c>.git</c> is tolerated).
    /// </summary>
    public static RepositoryPath? Parse(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length != 2 || segments.Any(string.IsNullOrEmpty))
        {
            return null;
        }

        var name = segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? segments[1][..^4] : segments[1];
        return name.Length == 0 ? null : new RepositoryPath(segments[0], name);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Owner}/{Name}";
}
