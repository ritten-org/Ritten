using Ritten.Changelogs;
using Ritten.Releases;

namespace Ritten.Tests.Changelogs;

/// <summary>
/// The entry classifies its own notes, so the version proposed and the reason given for it read
/// the same fact rather than each deciding for themselves.
/// </summary>
public class ChangelogEntryTests
{
    [Fact]
    public void NotesThatRemoveOrChangeWhatShippedAreBreaking()
    {
        new ChangelogEntry { Removed = ["A thing."] }.ReleaseKind.ShouldBe(ReleaseKind.Breaking);
        new ChangelogEntry { Changed = ["A thing."] }.ReleaseKind.ShouldBe(ReleaseKind.Breaking);

        // Breaking outranks the rest: one removal sizes the whole release.
        new ChangelogEntry { Added = ["A thing."], Removed = ["Another."] }.ReleaseKind.ShouldBe(ReleaseKind.Breaking);
    }

    [Fact]
    public void NotesThatAddToWhatShippedAreFeatures()
    {
        new ChangelogEntry { Added = ["A thing."] }.ReleaseKind.ShouldBe(ReleaseKind.Features);

        // Deprecating announces a removal without making one, so nothing breaks yet.
        new ChangelogEntry { Deprecated = ["A thing."] }.ReleaseKind.ShouldBe(ReleaseKind.Features);
        new ChangelogEntry { Added = ["A thing."], Fixed = ["Another."] }.ReleaseKind.ShouldBe(ReleaseKind.Features);
    }

    [Fact]
    public void NotesThatOnlyFixWhatShippedAreFixes()
    {
        new ChangelogEntry { Fixed = ["A thing."] }.ReleaseKind.ShouldBe(ReleaseKind.Fixes);
        new ChangelogEntry { Security = ["A thing."] }.ReleaseKind.ShouldBe(ReleaseKind.Fixes);

        // Prose under the heading with no sections at all still describes something.
        new ChangelogEntry { Body = "Some prose." }.ReleaseKind.ShouldBe(ReleaseKind.Fixes);
    }

    [Fact]
    public void EmptyNotesReleaseNothing()
    {
        new ChangelogEntry().ReleaseKind.ShouldBe(ReleaseKind.None);
    }

    [Fact]
    public void MergingGathersTheNotesUnderOneSetOfSections()
    {
        var existing = ChangelogParser.ParseEntry("### Added\n\n- A shipped thing.\n\n### Fixed\n\n- An old fix.");
        var later = ChangelogParser.ParseEntry("### Fixed\n\n- A later fix.\n\n### Added\n\n- A later thing.");

        var merged = existing.Merge(later);

        merged.Added.ShouldBe(["A shipped thing.", "A later thing."]);
        merged.Fixed.ShouldBe(["An old fix.", "A later fix."]);

        // The sections held every line of both bodies, so the entry renders as one set of them —
        // the order the author wrote their own sections in is not a reason to keep two.
        ChangelogRenderer.RenderEntry(merged).ShouldBe("### Added\n\n- A shipped thing.\n- A later thing.\n\n### Fixed\n\n- An old fix.\n- A later fix.");
    }

    [Fact]
    public void MergingKeepsBothBodiesWhenTheSectionsCannotHoldThem()
    {
        // "Notes" is not one of the six, so it lives on the body alone: rebuilding from the
        // sections would drop the heading and everything under it.
        var existing = ChangelogParser.ParseEntry("### Notes\n\n- Something the format has no section for.");
        var later = ChangelogParser.ParseEntry("### Fixed\n\n- A later fix.");

        var rendered = ChangelogRenderer.RenderEntry(existing.Merge(later));

        rendered.ShouldContain("### Notes");
        rendered.ShouldContain("- Something the format has no section for.");
        rendered.ShouldContain("- A later fix.");
    }

    [Fact]
    public void MergingIntoAnEntryWithNothingInItKeepsTheNotesArriving()
    {
        var merged = new ChangelogEntry().Merge(ChangelogParser.ParseEntry("### Fixed\n\n- A later fix."));

        merged.Fixed.ShouldBe(["A later fix."]);
        ChangelogRenderer.RenderEntry(merged).ShouldBe("### Fixed\n\n- A later fix.");
    }
}
