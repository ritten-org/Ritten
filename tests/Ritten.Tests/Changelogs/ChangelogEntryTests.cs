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
}
