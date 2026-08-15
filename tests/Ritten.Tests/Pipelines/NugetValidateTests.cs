using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The gate that stops a version being published twice.
/// </summary>
public class NugetValidateTests
{
    private readonly IPipelineLog _log = Substitute.For<IPipelineLog>();
    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly NuGetOptions _options = TestOptions.NuGet();

    public NugetValidateTests()
    {
        _report.Section("Release").Returns(_releaseSection);
        Published();
    }

    [Fact]
    public async Task PassesAtRestWhenTheVersionIsTheLatestPublished()
    {
        // Nothing is staged to release; new work accrues under [Unreleased] until a release is prepared.
        Published("1.0.0", "1.1.0", "1.2.0");

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Value.ShouldNotBeNull().Kind.ShouldBe(ReleaseStateKind.LatestInLine);
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("latest published version");
    }

    [Fact]
    public async Task PassesAtRestAtTheTipOfAnOlderLine()
    {
        // A release branch sitting at its line's tip is at rest, even with a newer major out.
        Published("1.2.0", "2.0.0");

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Value.ShouldNotBeNull().Kind.ShouldBe(ReleaseStateKind.LatestInLine);
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("latest overall");
    }

    [Fact]
    public async Task FailsWhenThePublishedVersionIsBehindItsOwnLine()
    {
        // 1.2.0 shipped, then the version was wound back while 1.3.0 went out: incoherent.
        Published("1.2.0", "1.3.0");

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("1.3.0");
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("already published");
    }

    [Fact]
    public async Task AllowsABackportToAnOlderMajorLine()
    {
        // 2.0.0 being out doesn't stop a security fix shipping to the 1.x line.
        Published("1.0.0", "2.0.0");

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        var release = result.Value.ShouldNotBeNull();
        release.Kind.ShouldBe(ReleaseStateKind.Releasable);
        release.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.0.0"));
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("backport");
    }

    [Fact]
    public async Task FailsWhenTheVersionIsBehindItsOwnLine()
    {
        Published("1.0.0", "1.5.0");

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.5.0");
    }

    [Fact]
    public async Task BlocksAnOlderMinorByDefault()
    {
        // Under SemVer, 1.2.6 with 1.3.4 out is a fix nobody needs — take 1.3.5 instead.
        Published("1.2.5", "1.3.4");

        var result = await Step().Run(Project("1.2.6"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("1.3.4");
    }

    [Fact]
    public async Task AllowsAnOlderMinorWhenLinesAreScopedToMinor()
    {
        // For projects that treat the major as a product version, minors are the real lines.
        _options.Lines = ReleaseLine.Minor;
        Published("1.2.5", "1.3.4");

        var result = await Step().Run(Project("1.2.6"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        var release = result.Value.ShouldNotBeNull();
        release.Kind.ShouldBe(ReleaseStateKind.Releasable);
        release.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.2.5"));
    }

    [Fact]
    public async Task ComparesBySemanticVersionRatherThanOrderOfArrival()
    {
        // 1.10.0 is newer than 1.9.0, however the feed happens to return them.
        Published("1.10.0", "1.2.0");

        var result = await Step().Run(Project("1.9.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task TreatsAPrereleaseAsBehindItsRelease()
    {
        Published("1.2.0");

        var result = await Step().Run(Project("1.2.0-beta.1"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SucceedsWhenTheVersionIsAheadOfTheLatestPublished()
    {
        Published("1.0.0", "1.1.0");

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        var release = result.Value.ShouldNotBeNull();
        release.Kind.ShouldBe(ReleaseStateKind.Releasable);
        release.LatestVersionInLine.ShouldBe(NuGetVersion.Parse("1.1.0"));
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.1.0");
    }

    [Fact]
    public async Task SucceedsForTheFirstEverVersion()
    {
        Published();

        var result = await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        var release = result.Value.ShouldNotBeNull();
        release.Kind.ShouldBe(ReleaseStateKind.Releasable);
        release.LatestVersionInLine.ShouldBeNull();
        release.LatestVersion.ShouldBeNull();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("first published version");
    }

    [Fact]
    public async Task ChecksTheConfiguredFeedForTheProjectsOwnPackage()
    {
        await Step().Run(Project("1.2.0"), TestContext.Current.CancellationToken);

        await _nuget.Received().GetPublishedVersions(
            Arg.Is<NuGetFeed>(f => f.Url == _options.Feed),
            "My.Package",
            Arg.Any<CancellationToken>());
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private void Published(params string[] versions) =>
        _nuget.GetPublishedVersions(Arg.Any<NuGetFeed>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([.. versions.Select(NuGetVersion.Parse)]);

    private NugetValidate Step() =>
        new(_log, Options.Create(_options), _report, _nuget);
}
