using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

public class CheckVersionTests
{
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _versionSection = new("Version");
    private readonly NuGetOptions _options = TestOptions.NuGet();

    public CheckVersionTests()
    {
        _report.Section("Version").Returns(_versionSection);
    }

    [Fact]
    public void FailsAHistoricVersion()
    {
        var state = new ReleaseState(Published: true, LatestInLine: false, NuGetVersion.Parse("1.3.0"), NuGetVersion.Parse("1.3.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("already published");
        _versionSection.Tone.ShouldBe(ReportTone.Failure);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.3.0");
    }

    [Fact]
    public void FailsASupersededVersion()
    {
        var state = new ReleaseState(Published: false, LatestInLine: false, NuGetVersion.Parse("1.5.0"), NuGetVersion.Parse("1.5.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("must be higher than");
        _versionSection.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void NamesTheLineWhenItIsNotTheWholeStory()
    {
        // A single-line project stays unqualified; a backport line is called out.
        var state = new ReleaseState(Published: false, LatestInLine: false, NuGetVersion.Parse("1.5.0"), NuGetVersion.Parse("2.0.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("on the 1.x line");
    }

    [Fact]
    public void PassesTheLatestInItsLine()
    {
        var state = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Tone.ShouldBe(ReportTone.Success);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("latest published version");
    }

    [Fact]
    public void PassesTheTipOfAnOlderLine()
    {
        var state = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("2.0.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("latest overall");
    }

    [Fact]
    public void PassesAReleasableVersion()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Tone.ShouldBe(ReportTone.Success);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.1.0");
    }

    [Fact]
    public void PassesTheFirstEverVersion()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, null, null);

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("first published version");
    }

    [Fact]
    public void PassesABackportAndCallsItOne()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("2.0.0"));

        var result = Step().Run(Project("1.2.0"), state);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("backport");
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private CheckVersion Step() =>
        new(Options.Create(_options), _report);
}
