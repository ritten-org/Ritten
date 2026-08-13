namespace Ritten.Changelogs;

/// <summary>
/// Computes the reference-style version links a changelog should have.
/// </summary>
internal static class ChangelogLinkGenerator
{
    public static IReadOnlyCollection<ChangelogLink> Generate(Changelog changelog, ChangelogRepository repository)
    {
        var url = repository.Url.TrimEnd('/');
        var released = changelog.Entries
            .Where(e => e.Version != null)
            .Select(e => e.Version!)
            .OrderByDescending(v => v)
            .ToList();

        var links = new List<ChangelogLink>();
        if (changelog.Unreleased != null)
        {
            links.Add(new ChangelogLink("Unreleased", released.Count > 0
                ? $"{url}/compare/{repository.TagPrefix}{released[0]}...HEAD"
                : $"{url}/commits/HEAD"));
        }

        for (var i = 0; i < released.Count; i++)
        {
            var version = released[i];
            var previous = i + 1 < released.Count ? released[i + 1] : null;
            links.Add(new ChangelogLink(version.ToString(), previous is null
                ? $"{url}/releases/tag/{repository.TagPrefix}{version}"
                : $"{url}/compare/{repository.TagPrefix}{previous}...{repository.TagPrefix}{version}"));
        }

        return links;
    }
}
