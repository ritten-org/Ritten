using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Pipelines;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The deploy-only gate that ends the job early when there is nothing to release. Rerunning a
/// completed deploy is reassurance, not an error, so the early stop is a success: CI tracks
/// failures, and "no work to do" isn't one.
/// </summary>
public class ReleasableGateTests
{
    [Fact]
    public async Task ContinuesWhenTheProjectIsReleasable()
    {
        var result = await Step().Run(new ReleaseState(Published: false, LatestInLine: true, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        result.Continue.ShouldBeTrue();
    }

    [Fact]
    public async Task StopsSuccessfullyWhenThisVersionIsAlreadyReleased()
    {
        // `deploy && deploy` exits 0 both times: the second run has nothing to do, and says so.
        var release = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

        var result = await Step().Run(release, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        result.Continue.ShouldBeFalse();
    }

    private static ReleasableGate Step() => new(Substitute.For<IPipelineLog>());
}
