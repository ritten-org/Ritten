using NuGet.Versioning;
using Ritten.Releases;

namespace Ritten.Tests.Releases;

/// <summary>
/// Version policy, readable without a changelog anywhere in sight.
/// </summary>
public class NuGetVersionExtensionsTests
{
    [Fact]
    public void FixesTakeThePatch()
    {
        Next("1.2.3", ReleaseKind.Fixes).ShouldBe("1.2.4");
    }

    [Fact]
    public void FeaturesTakeTheMinor()
    {
        Next("1.2.3", ReleaseKind.Features).ShouldBe("1.3.0");
    }

    [Fact]
    public void BreakingChangesTakeTheMajor()
    {
        Next("1.2.3", ReleaseKind.Breaking).ShouldBe("2.0.0");
    }

    [Fact]
    public void BreakingChangesRideTheMinorBeforeOneOh()
    {
        // SemVer's own advice for 0.x: the major is intent, not a compatibility promise.
        Next("0.7.0", ReleaseKind.Breaking).ShouldBe("0.8.0");
        Next("0.7.0", ReleaseKind.Features).ShouldBe("0.8.0");
        Next("0.7.3", ReleaseKind.Fixes).ShouldBe("0.7.4");
    }

    [Fact]
    public void FinishingAPrereleaseIsTheRelease()
    {
        Next("1.3.0-beta.2", ReleaseKind.Features).ShouldBe("1.3.0");
    }

    [Fact]
    public void NothingToReleaseTakesThePatch()
    {
        // There's nothing to size the bump by, and a patch is the smallest thing it could be.
        Next("1.2.3", ReleaseKind.None).ShouldBe("1.2.4");
    }

    private static string Next(string current, ReleaseKind kind) => NuGetVersion.Parse(current).Next(kind).ToString();
}
