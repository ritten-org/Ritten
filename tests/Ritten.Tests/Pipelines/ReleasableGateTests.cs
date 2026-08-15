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
    private readonly IPipelineState _state = Substitute.For<IPipelineState>();

    [Fact]
    public async Task ContinuesWhenTheProjectIsReleasable()
    {
        _state.Get<ReleaseState>().Returns(ReleaseState.Releasable(null, null));

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        result.Continue.ShouldBeTrue();
    }

    [Fact]
    public async Task StopsSuccessfullyWhenTheProjectIsAtRest()
    {
        // `deploy && deploy` exits 0 both times: the second run has nothing to do, and says so.
        _state.Get<ReleaseState>()
            .Returns(ReleaseState.LatestInLine(NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0")));

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        result.Continue.ShouldBeFalse();
    }

    [Fact]
    public async Task FailsWithoutTheReleaseStateInState()
    {
        _state.Get<ReleaseState>().Returns((ReleaseState?)null);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("Release state");
    }

    private ReleasableGate Step() => new(_state, Substitute.For<IPipelineLog>());
}
