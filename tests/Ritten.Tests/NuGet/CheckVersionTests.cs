using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

public class CheckVersionTests
{
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _versionSection = new(SectionName.Version);
    private readonly NuGetOptions _options = TestOptions.NuGet();

    public CheckVersionTests()
    {
        _report.Section(SectionName.Version).Returns(_versionSection);
    }

    [Fact]
    public void FailsAHistoricVersion()
    {
        var state = new ReleaseState(Published: true, LatestInLine: false, NuGetVersion.Parse("1.3.0"), NuGetVersion.Parse("1.3.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("already published");
        _versionSection.Tone.ShouldBe(ReportTone.Failure);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.3.0");
    }

    [Fact]
    public void FailsASupersededVersion()
    {
        var state = new ReleaseState(Published: false, LatestInLine: false, NuGetVersion.Parse("1.5.0"), NuGetVersion.Parse("1.5.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("must be higher than");
        _versionSection.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void NamesTheLineWhenItIsNotTheWholeStory()
    {
        // A single-line project stays unqualified; a backport line is called out.
        var state = new ReleaseState(Published: false, LatestInLine: false, NuGetVersion.Parse("1.5.0"), NuGetVersion.Parse("2.0.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("on the 1.x line");
    }

    [Fact]
    public void PassesTheLatestInItsLine()
    {
        var state = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Tone.ShouldBe(ReportTone.Success);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("latest published version");
    }

    [Fact]
    public void PassesTheTipOfAnOlderLine()
    {
        var state = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("2.0.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("latest overall");
    }

    [Fact]
    public void PassesAReleasableVersion()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Tone.ShouldBe(ReportTone.Success);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("1.1.0");
    }

    [Fact]
    public void PassesTheFirstEverVersion()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, null, null);

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("first published version");
    }

    [Fact]
    public void PassesABackportAndCallsItOne()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("2.0.0"));

        var result = Step().Run(Project("1.2.0"), state, null);

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("backport");
    }

    [Fact]
    public void PassesAnUnchangedPublishedVersionUnderACuratedCadence()
    {
        // Curated: a maintainer releases when they choose, so a merge may leave the version at rest.
        var result = Step().Run(Project("1.2.0"), AtRest(), Changed("src/My.Package/Thing.cs"));

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void FailsAChangedPublishedVersionUnderAContinuousCadence()
    {
        _options.Cadence = ReleaseCadence.Continuous;

        var result = Step().Run(Project("1.2.0"), AtRest(), Changed("src/My.Package/Thing.cs"));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("bump <Version>");
        _versionSection.Tone.ShouldBe(ReportTone.Failure);
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("the merge is the release");
    }

    [Fact]
    public void PassesAnUnchangedPublishedVersionUnderAContinuousCadence()
    {
        _options.Cadence = ReleaseCadence.Continuous;

        var result = Step().Run(Project("1.2.0"), AtRest(), Changed());

        result.IsFailure.ShouldBeFalse();
        _versionSection.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("nothing new to release");
    }

    [Fact]
    public void PassesOutsideAPullRequestUnderAContinuousCadence()
    {
        // The deploy after the merge: nothing to measure against, and the releasable gate decides what ships.
        _options.Cadence = ReleaseCadence.Continuous;

        var result = Step().Run(Project("1.2.0"), AtRest(), ShippedChanges.Unreviewed);

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void PassesABumpedVersionUnderAContinuousCadence()
    {
        _options.Cadence = ReleaseCadence.Continuous;
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

        var result = Step().Run(Project("1.3.0"), state, Changed("src/My.Package/Thing.cs"));

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void FailsAChangedPartlyPublishedVersionUnderAContinuousCadence()
    {
        // A new package joining a lockstep release: the others already hold this number with the old code.
        _options.Cadence = ReleaseCadence.Continuous;
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"))
        {
            Packages = [new PackagePublication("My.Package", Published: true), new PackagePublication("My.Package.New", Published: false)]
        };

        var result = Step().Run(Project("1.2.0"), state, Changed("src/My.Package/Thing.cs"));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void RefusesAContinuousCadenceWithoutTheChanges()
    {
        // Passing without the diff would be the very silence the cadence exists to prevent.
        _options.Cadence = ReleaseCadence.Continuous;

        var result = Step().Run(Project("1.2.0"), AtRest(), null);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(nameof(ReadShippedChanges));
    }

    private static ReleaseState AtRest() =>
        new(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

    private static ShippedChanges Changed(params string[] files) => new("main", files);

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private CheckVersion Step() =>
        new(Options.Create(_options), _report);
}
