using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Workflows;

namespace Ritten.Tests.Changelogs;

public class CheckChangelogEntryTests
{
    // The real client, so these tests exercise the actual parser.
    private static readonly IChangelog Changelogs = WorkflowRunBuilderHelpers.Create()
        .AddChangelogs(new ChangelogSettings())
        .Services.BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private static readonly ReleaseState Releasable = new(Published: false, LatestInLine: true, null, null);
    private static readonly ReleaseState AlreadyPublished =
        new(Published: true, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));

    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _changelogSection = new("Changelog");

    public CheckChangelogEntryTests()
    {
        _report.Section("Changelog").Returns(_changelogSection);
    }

    [Fact]
    public void APublishedVersionNeedsNoEntry()
    {
        // Nothing is being released, so nothing has to be documented yet.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.1.0] - 2026-08-01

            - An older change.
            """);

        var result = Step().Run(Project("1.2.0"), AlreadyPublished, changelog);

        result.IsFailure.ShouldBeFalse();
        _changelogSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public void PassesWhenTheEntryIsPresent()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.
            """);

        var result = Step().Run(Project("1.2.0"), Releasable, changelog);

        result.IsFailure.ShouldBeFalse();
        _changelogSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public void FailsWhenTheEntryIsMissing()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.1.0] - 2026-08-01

            - An older change.
            """);

        var result = Step().Run(Project("1.2.0"), Releasable, changelog);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("1.2.0");
        _changelogSection.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void PassesForAPrereleaseUsingTheUnreleasedEntry()
    {
        // Nothing writes a versioned heading before it ships, so a 0.x release reads [Unreleased].
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [Unreleased]

            - A change.
            """);

        var result = Step().Run(Project("1.0.0-beta.1"), Releasable, changelog);

        result.IsFailure.ShouldBeFalse();
        _changelogSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public void FailsForAPrereleaseWithoutAnUnreleasedEntry()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.
            """);

        var result = Step().Run(Project("1.0.0-beta.1"), Releasable, changelog);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("[Unreleased]");
    }

    [Fact]
    public void FailsWhenTheEntryIsEmpty()
    {
        // An empty entry would ship a release with empty notes.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [Unreleased]
            """);

        var result = Step().Run(Project("1.0.0-beta.1"), Releasable, changelog);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("empty");
    }

    [Fact]
    public void TreatsAnUnlabelledZeroPointVersionAsARelease()
    {
        // 0.0.1 has no prerelease label, so a feed serves it as the latest stable version and
        // people get it without asking for prereleases. It earns its own entry like any release.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [Unreleased]

            - A change.
            """);

        var result = Step().Run(Project("0.0.1"), Releasable, changelog);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("0.0.1");
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private CheckChangelogEntry Step() =>
        new(Substitute.For<IWorkflowLog>(), _report);
}
