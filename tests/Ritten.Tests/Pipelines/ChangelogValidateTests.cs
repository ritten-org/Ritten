using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Core.Settings;
using Ritten.DotNet;
using Ritten.Extensions;
using Ritten.Pipelines;
using Ritten.Reporting;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The documentation obligation attaches to releasing: an unpublished version must have its
/// entry, and a published one owes nothing.
/// </summary>
public class ChangelogValidateTests
{
    // The real client, so these tests exercise the actual parser.
    private static readonly IChangelog Changelogs = new ServiceCollection()
        .AddChangelogs(new ChangelogSettings())
        .BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private static readonly ReleaseState Releasable = new(Published: false, LatestInLine: true, null, null);
    private static readonly ReleaseState AlreadyPublished =
        new(Published: true, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));

    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");

    public ChangelogValidateTests()
    {
        _report.Section("Release").Returns(_releaseSection);
    }

    [Fact]
    public async Task APublishedVersionNeedsNoEntry()
    {
        // Nothing is being released, so nothing has to be documented yet.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.1.0] - 2026-08-01

            - An older change.
            """);

        var result = await Step().Run(Project("1.2.0"), AlreadyPublished, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task PassesWhenTheEntryIsPresent()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.
            """);

        var result = await Step().Run(Project("1.2.0"), Releasable, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task FailsWhenTheEntryIsMissing()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.1.0] - 2026-08-01

            - An older change.
            """);

        var result = await Step().Run(Project("1.2.0"), Releasable, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("1.2.0");
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public async Task PassesForAPrereleaseUsingTheUnreleasedEntry()
    {
        // Nothing writes a versioned heading before it ships, so a 0.x release reads [Unreleased].
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [Unreleased]

            - A change.
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task FailsForAPrereleaseWithoutAnUnreleasedEntry()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("[Unreleased]");
    }

    [Fact]
    public async Task FailsWhenTheEntryIsEmpty()
    {
        // An empty entry would ship a release with empty notes.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [Unreleased]
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("empty");
    }

    [Fact]
    public async Task TreatsAnUnlabelledZeroPointVersionAsARelease()
    {
        // 0.0.1 has no prerelease label, so a feed serves it as the latest stable version and
        // people get it without asking for prereleases. It earns its own entry like any release.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [Unreleased]

            - A change.
            """);

        var result = await Step().Run(Project("0.0.1"), Releasable, changelog, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("0.0.1");
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private ChangelogValidate Step() =>
        new(Substitute.For<IPipelineLog>(), _report);
}
