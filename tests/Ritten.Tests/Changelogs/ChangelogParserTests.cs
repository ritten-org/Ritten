using NuGet.Versioning;
using Ritten.Changelogs;

namespace Ritten.Tests.Changelogs;

public class ChangelogParserTests
{
    [Fact]
    public Task Parse_ReadsTheFullDocument() =>
        Verify(ChangelogParser.Parse(SampleChangelog.Text));

    [Fact]
    public void Entry_FindsTheEntryForAVersion()
    {
        var changelog = ChangelogParser.Parse(SampleChangelog.Text);

        changelog.Entry(NuGetVersion.Parse("1.2.0")).ShouldNotBeNull();
        changelog.Entry(NuGetVersion.Parse("9.9.9")).ShouldBeNull();
    }

    [Fact]
    public void Unreleased_FindsTheEntryWithNoVersion()
    {
        var changelog = ChangelogParser.Parse(SampleChangelog.Text);

        changelog.Unreleased.ShouldNotBeNull();
        changelog.Unreleased.Version.ShouldBeNull();
    }

    [Fact]
    public void Parse_TreatsAnEntryWithNoContentAsEmpty()
    {
        var changelog = ChangelogParser.Parse("## [Unreleased]\n\n## [1.0.0] - 2026-01-01\n\n- Released.\n");

        changelog.Unreleased!.IsEmpty.ShouldBeTrue();
        changelog.Entry(NuGetVersion.Parse("1.0.0"))!.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Parse_AllowsAHeadingWithNoDate()
    {
        var changelog = ChangelogParser.Parse("## [1.0.0]\n\n- Released.\n");

        changelog.Entries.Single().Date.ShouldBeNull();
    }

    [Fact]
    public void Parse_ThrowsForAnInvalidVersionHeading()
    {
        var exception = Should.Throw<FormatException>(() => ChangelogParser.Parse("## [not-a-version]\n"));

        exception.Message.ShouldContain("not-a-version");
    }

    [Fact]
    public Task ParseEntry_ReadsABareBody() =>
        Verify(ChangelogParser.ParseEntry("### Added\n\n- A thing.\n"));

    [Fact]
    public Task ParseEntry_ReadsALeadingHeading() =>
        Verify(ChangelogParser.ParseEntry("## [2.0.0] - 2026-01-02\n\n### Removed\n\n- Old thing.\n"));
}
