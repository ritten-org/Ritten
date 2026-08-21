using NuGet.Versioning;
using Ritten.Changelogs;

namespace Ritten.Tests.Changelogs;

public class VersionProposalTests
{
    [Fact]
    public void FixesOnlyTakeThePatch()
    {
        Next("1.2.3", new ChangelogEntry { Fixed = ["Something."] }).ShouldBe("1.2.4");
    }

    [Fact]
    public void AdditionsTakeTheMinor()
    {
        Next("1.2.3", new ChangelogEntry { Added = ["Something."], Fixed = ["Something else."] }).ShouldBe("1.3.0");
    }

    [Fact]
    public void ChangesAndRemovalsTakeTheMajor()
    {
        Next("1.2.3", new ChangelogEntry { Changed = ["Something."] }).ShouldBe("2.0.0");
        Next("1.2.3", new ChangelogEntry { Removed = ["Something."] }).ShouldBe("2.0.0");
    }

    [Fact]
    public void BreakingChangesRideTheMinorBeforeOneOh()
    {
        // SemVer's own advice for 0.x: the major is intent, not a compatibility promise.
        Next("0.7.0", new ChangelogEntry { Changed = ["Something."] }).ShouldBe("0.8.0");
        Next("0.7.0", new ChangelogEntry { Added = ["Something."] }).ShouldBe("0.8.0");
        Next("0.7.3", new ChangelogEntry { Fixed = ["Something."] }).ShouldBe("0.7.4");
    }

    [Fact]
    public void FinishingAPrereleaseIsTheRelease()
    {
        Next("1.3.0-beta.2", new ChangelogEntry { Added = ["Something."] }).ShouldBe("1.3.0");
    }

    [Fact]
    public void NothingUnreleasedTakesThePatch()
    {
        // There's nothing to size the bump by, and a patch is the smallest thing it could be.
        Next("1.2.3", null).ShouldBe("1.2.4");
        Next("1.2.3", new ChangelogEntry()).ShouldBe("1.2.4");
    }

    private static string Next(string current, ChangelogEntry? unreleased) =>
        VersionProposal.Next(NuGetVersion.Parse(current), unreleased).ToString();
}
