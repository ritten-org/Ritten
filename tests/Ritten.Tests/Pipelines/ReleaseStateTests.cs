using NuGet.Versioning;
using Ritten.Pipelines;

namespace Ritten.Tests.Pipelines;

public class ReleaseStateTests
{
    [Fact]
    public void OnLatestLine_WhenTheLinesTipIsTheLatestOverall()
    {
        var state = new ReleaseState(
            Published: true, LatestInLine: true,
            NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

        state.OnLatestLine.ShouldBeTrue();
    }

    [Fact]
    public void NotOnLatestLine_WhenANewerLineExists()
    {
        var state = new ReleaseState(
            Published: true, LatestInLine: true,
            NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("2.0.0"));

        state.OnLatestLine.ShouldBeFalse();
    }

    [Fact]
    public void OnLatestLine_WhenNothingHasBeenPublished()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, null, null);

        state.OnLatestLine.ShouldBeTrue();
    }

    [Fact]
    public void NotOnLatestLine_ForAFreshLineOpenedBelowExistingReleases()
    {
        // A backport line with no releases of its own yet: minor-scoped 1.1.x while 2.0.0 is out.
        var state = new ReleaseState(Published: false, LatestInLine: true, null, NuGetVersion.Parse("2.0.0"));

        state.OnLatestLine.ShouldBeFalse();
    }
}
