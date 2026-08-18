using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

/// <summary>
/// The classification is total: every version maps to a state, coherent or not, and nothing
/// here fails on policy — judging belongs to <see cref="CheckVersion"/>.
/// </summary>
public class NugetReadTests
{
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly NuGetOptions _options = TestOptions.NuGet();

    public NugetReadTests()
    {
        Published();
    }

    [Fact]
    public async Task TheLatestPublishedVersionIsLatestInLine()
    {
        Published("1.0.0", "1.1.0", "1.2.0");

        var state = await Classify("1.2.0");

        state.Published.ShouldBeTrue();
        state.LatestInLine.ShouldBeTrue();
    }

    [Fact]
    public async Task TheTipOfAnOlderLineIsLatestInLine()
    {
        // A release branch sitting at its line's tip is at rest, even with a newer major out.
        Published("1.2.0", "2.0.0");

        var state = await Classify("1.2.0");

        state.Published.ShouldBeTrue();
        state.LatestInLine.ShouldBeTrue();
        state.LatestVersion.ShouldBe(NuGetVersion.Parse("2.0.0"));
    }

    [Fact]
    public async Task APublishedVersionBehindItsOwnLineIsHistoric()
    {
        // 1.2.0 shipped, then the version was wound back while 1.3.0 went out.
        Published("1.2.0", "1.3.0");

        var state = await Classify("1.2.0");

        state.Published.ShouldBeTrue();
        state.LatestInLine.ShouldBeFalse();
        state.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.3.0"));
    }

    [Fact]
    public async Task AnUnpublishedVersionBehindItsOwnLineIsSuperseded()
    {
        Published("1.0.0", "1.5.0");

        var state = await Classify("1.2.0");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeFalse();
        state.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.5.0"));
    }

    [Fact]
    public async Task ABackportToAnOlderMajorLineIsReleasable()
    {
        // 2.0.0 being out doesn't stop a security fix shipping to the 1.x line.
        Published("1.0.0", "2.0.0");

        var state = await Classify("1.2.0");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeTrue();
        state.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.0.0"));
    }

    [Fact]
    public async Task AnOlderMinorIsSupersededByDefault()
    {
        // Under SemVer, 1.2.6 with 1.3.4 out is a fix nobody needs — take 1.3.5 instead.
        Published("1.2.5", "1.3.4");

        var state = await Classify("1.2.6");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeFalse();
    }

    [Fact]
    public async Task AnOlderMinorIsReleasableWhenLinesAreScopedToMinor()
    {
        // For projects that treat the major as a product version, minors are the real lines.
        _options.Lines = ReleaseLine.Minor;
        Published("1.2.5", "1.3.4");

        var state = await Classify("1.2.6");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeTrue();
        state.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.2.5"));
    }

    [Fact]
    public async Task ComparesBySemanticVersionRatherThanOrderOfArrival()
    {
        // 1.10.0 is newer than 1.9.0, however the feed happens to return them.
        Published("1.10.0", "1.2.0");

        var state = await Classify("1.9.0");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeFalse();
    }

    [Fact]
    public async Task TreatsAPrereleaseAsBehindItsRelease()
    {
        Published("1.2.0");

        var state = await Classify("1.2.0-beta.1");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeFalse();
    }

    [Fact]
    public async Task AVersionAheadOfItsLineIsReleasable()
    {
        Published("1.0.0", "1.1.0");

        var state = await Classify("1.2.0");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeTrue();
        state.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.1.0"));
    }

    [Fact]
    public async Task TheFirstEverVersionIsReleasable()
    {
        Published();

        var state = await Classify("1.2.0");

        state.Published.ShouldBeFalse();
        state.LatestInLine.ShouldBeTrue();
        state.LatestVersionInLine.ShouldBeNull();
        state.LatestVersion.ShouldBeNull();
    }

    [Fact]
    public async Task ChecksTheConfiguredFeedForTheProjectsOwnPackage()
    {
        await Classify("1.2.0");

        await _nuget.Received().GetPublishedVersions(
            Arg.Is<NuGetFeed>(f => f.Url == _options.Feed),
            "My.Package",
            Arg.Any<CancellationToken>());
    }

    private async Task<ReleaseState> Classify(string version)
    {
        var project = new Project { Name = "My.Package", Version = NuGetVersion.Parse(version) };
        var result = await Step().Run(project, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        return result.Value.ShouldNotBeNull();
    }

    private void Published(params string[] versions) =>
        _nuget.GetPublishedVersions(Arg.Any<NuGetFeed>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([.. versions.Select(NuGetVersion.Parse)]);

    private NugetRead Step() =>
        new(_log, Options.Create(_options), _nuget);
}
