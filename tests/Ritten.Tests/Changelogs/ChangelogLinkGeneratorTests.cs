using NuGet.Versioning;
using Ritten.Changelogs;

namespace Ritten.Tests.Changelogs;

public class ChangelogLinkGeneratorTests
{
    private static readonly ChangelogRepository Repository = new("https://github.com/example/repo");

    [Fact]
    public void Generate_ChainsEachVersionToTheOneBeforeIt()
    {
        var changelog = ChangelogWith(unreleased: true, "1.2.0", "1.1.0-beta.1", "1.0.0");

        var links = ChangelogLinkGenerator.Generate(changelog, Repository);

        links.Select(l => l.ToMarkdown()).ShouldBe(
        [
            "[Unreleased]: https://github.com/example/repo/compare/v1.2.0...HEAD",
            "[1.2.0]: https://github.com/example/repo/compare/v1.1.0-beta.1...v1.2.0",
            "[1.1.0-beta.1]: https://github.com/example/repo/compare/v1.0.0...v1.1.0-beta.1",
            "[1.0.0]: https://github.com/example/repo/releases/tag/v1.0.0"
        ]);
    }

    [Fact]
    public void Generate_LinksAnUnreleasedOnlyChangelogToTheCommitHistory()
    {
        var changelog = ChangelogWith(unreleased: true);

        var links = ChangelogLinkGenerator.Generate(changelog, Repository);

        links.Select(l => l.ToMarkdown()).ShouldBe(["[Unreleased]: https://github.com/example/repo/commits/HEAD"]);
    }

    [Fact]
    public void Generate_OmitsTheUnreleasedLinkWhenThereIsNoUnreleasedEntry()
    {
        var changelog = ChangelogWith(unreleased: false, "1.0.0");

        var links = ChangelogLinkGenerator.Generate(changelog, Repository);

        links.Select(l => l.Label).ShouldBe(["1.0.0"]);
    }

    [Fact]
    public void Generate_OrdersByVersionRatherThanFileOrder()
    {
        var changelog = ChangelogWith(unreleased: false, "1.0.0", "2.0.0");

        var links = ChangelogLinkGenerator.Generate(changelog, Repository);

        links.Select(l => l.ToMarkdown()).ShouldBe(
        [
            "[2.0.0]: https://github.com/example/repo/compare/v1.0.0...v2.0.0",
            "[1.0.0]: https://github.com/example/repo/releases/tag/v1.0.0"
        ]);
    }

    [Fact]
    public void Generate_HonoursTheTagPrefixAndTrimsTheUrl()
    {
        var changelog = ChangelogWith(unreleased: false, "1.0.0");
        var repository = new ChangelogRepository("https://github.com/example/repo/") { TagPrefix = "" };

        var links = ChangelogLinkGenerator.Generate(changelog, repository);

        links.Single().Url.ShouldBe("https://github.com/example/repo/releases/tag/1.0.0");
    }

    private static Changelog ChangelogWith(bool unreleased, params string[] versions)
    {
        var entries = new List<ChangelogEntry>();
        if (unreleased)
        {
            entries.Add(new ChangelogEntry { Added = ["Upcoming."] });
        }

        entries.AddRange(versions.Select(v => new ChangelogEntry { Version = NuGetVersion.Parse(v), Added = ["A change."] }));
        return new Changelog { Entries = entries };
    }
}
