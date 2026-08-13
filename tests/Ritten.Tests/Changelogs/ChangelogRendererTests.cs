using NuGet.Versioning;
using Ritten.Changelogs;

namespace Ritten.Tests.Changelogs;

public class ChangelogRendererTests
{
    [Fact]
    public void Render_RoundTripsAParsedChangelog()
    {
        var changelog = ChangelogParser.Parse(SampleChangelog.Text);

        ChangelogRenderer.Render(changelog).ShouldBe(SampleChangelog.Text);
    }

    [Fact]
    public Task RenderEntry_UsesTheAuthoredBodyVerbatim()
    {
        var entry = ChangelogParser.Parse(SampleChangelog.Text).Entry(NuGetVersion.Parse("1.2.0"))!;

        return Verify(ChangelogRenderer.RenderEntry(entry));
    }

    [Fact]
    public Task RenderEntry_SynthesizesFromSectionsWhenThereIsNoBody()
    {
        var entry = new ChangelogEntry
        {
            Preamble = "A summary.",
            Added = ["New thing."],
            Fixed = ["Broken thing.", "Other broken thing."]
        };

        return Verify(ChangelogRenderer.RenderEntry(entry));
    }

    [Fact]
    public Task Render_SynthesizesAChangelogBuiltInCode()
    {
        var changelog = new Changelog
        {
            Preamble = "# Changelog",
            Entries =
            [
                new ChangelogEntry { Added = ["Upcoming."] },
                new ChangelogEntry
                {
                    Version = NuGetVersion.Parse("1.0.0"),
                    Date = new DateOnly(2026, 8, 12),
                    Added = ["Initial release."]
                }
            ]
        };

        return Verify(ChangelogRenderer.Render(changelog));
    }
}
