using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The gate that stops a version being published twice. Pushing to a feed can't be undone — a
/// version can be delisted but never unpublished — so this is the check that most needs to hold.
/// </summary>
public class NugetValidateTests
{
    private readonly IPipelineLog _log = Substitute.For<IPipelineLog>();
    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly IPipelineState _state = Substitute.For<IPipelineState>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly NuGetOptions _options = TestOptions.NuGet();

    public NugetValidateTests()
    {
        _report.Section("Release").Returns(_releaseSection);
        _state.Get<Project>().Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") });
        Published();
    }

    [Fact]
    public async Task FailsWhenTheVersionIsAlreadyPublished()
    {
        Published("1.0.0", "1.1.0", "1.2.0");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("1.2.0");
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("already published");
    }

    [Fact]
    public async Task FailsWhenTheVersionIsBehindTheLatestPublished()
    {
        Published("1.0.0", "2.0.0");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("2.0.0");
    }

    [Fact]
    public async Task ComparesBySemanticVersionRatherThanOrderOfArrival()
    {
        // 1.10.0 is newer than 1.9.0, however the feed happens to return them.
        _state.Get<Project>().Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.9.0") });
        Published("1.10.0", "1.2.0");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task TreatsAPrereleaseAsBehindItsRelease()
    {
        _state.Get<Project>().Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0-beta.1") });
        Published("1.2.0");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SucceedsWhenTheVersionIsAheadOfTheLatestPublished()
    {
        Published("1.0.0", "1.1.0");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.1.0");
    }

    [Fact]
    public async Task SucceedsForTheFirstEverVersion()
    {
        Published();

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
        _releaseSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("first published version");
    }

    [Fact]
    public async Task SkipsTheCheckEntirelyWhenAskedTo()
    {
        // Dependabot can't bump a version, so its pull requests opt out.
        _options.SkipVersionCheck = true;

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _nuget.DidNotReceive().GetPublishedVersions(
            Arg.Any<NuGetFeed>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailsWithoutTheProjectInState()
    {
        _state.Get<Project>().Returns((Project?)null);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("Project info");
    }

    [Fact]
    public async Task ChecksTheConfiguredFeedForTheProjectsOwnPackage()
    {
        await Step().Run(TestContext.Current.CancellationToken);

        await _nuget.Received().GetPublishedVersions(
            Arg.Is<NuGetFeed>(f => f.Url == _options.Feed),
            "My.Package",
            Arg.Any<CancellationToken>());
    }

    private void Published(params string[] versions) =>
        _nuget.GetPublishedVersions(Arg.Any<NuGetFeed>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([.. versions.Select(NuGetVersion.Parse)]);

    private NugetValidate Step() =>
        new(_log, Options.Create(_options), _state, _report, _nuget);
}
