using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.Settings;
using Ritten.DotNet;
using Ritten.Extensions;
using Ritten.Pipelines;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class ChangelogValidateTests
{
    // The real client, so these tests exercise the actual parser and link generator.
    private static readonly IChangelog Changelogs = new ServiceCollection()
        .AddChangelogs(new ChangelogSettings())
        .BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private static readonly ReleaseState Releasable = ReleaseState.Releasable(null, null);
    private static readonly ReleaseState LatestInLine =
        ReleaseState.LatestInLine(NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly ChangelogOptions _options = TestOptions.Changelog();

    public ChangelogValidateTests()
    {
        _options.RepositoryUrl = "https://github.com/example/repo";
        _report.Section("Release").Returns(_releaseSection);
    }

    [Fact]
    public async Task LatestInLine_NeedsNoEntryForTheCurrentVersion()
    {
        // Nothing is being released, so nothing has to be documented yet.
        SetChangelog(
            """
            # Changelog

            ## [1.1.0] - 2026-08-01

            - An older change.

            [1.1.0]: https://github.com/example/repo/releases/tag/v1.1.0
            """);

        var result = await Step().Run(Project("1.2.0"), LatestInLine, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task LatestInLine_StillKeepsTheLinksCorrect()
    {
        // The links are deterministic in every state, so drift is never allowed to accumulate.
        SetChangelog(
            """
            # Changelog

            ## [1.1.0] - 2026-08-01

            - An older change.

            [1.1.0]: https://github.com/example/repo/releases/tag/v1.0.0
            """);

        var result = await Step().Run(Project("1.2.0"), LatestInLine, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("links");
    }

    [Fact]
    public async Task PassesWhenTheEntryAndLinksAreCorrect()
    {
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0
            """);

        await Step().Run(Project("1.2.0"), Releasable, TestContext.Current.CancellationToken);

        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task FailsWithThePasteableBlockWhenTheLinksAreStale()
    {
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.0.0
            """);

        var result = await Step().Run(Project("1.2.0"), Releasable, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        var failure = _releaseSection.Entries.OfType<ReportParagraph>().Last();
        failure.Markdown.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");
    }

    [Fact]
    public async Task OffersTheSameBlockToTheTerminalAsVerbatimContent()
    {
        // The terminal indents everything a step says, and a pasted leading space fails the very
        // check that printed the block — so it travels as verbatim content, rendered at the margin.
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.0.0
            """);

        var result = await Step().Run(Project("1.2.0"), Releasable, TestContext.Current.CancellationToken);

        var error = result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        var block = error.Verbatim.ShouldNotBeNull();
        block.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");
        block.Split('\n').ShouldAllBe(line => line == line.TrimStart());
    }

    [Fact]
    public async Task SkipsLinkValidationWithoutARepositoryUrl()
    {
        _options.RepositoryUrl = null;
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://example.com/completely-wrong
            """);

        await Step().Run(Project("1.2.0"), Releasable, TestContext.Current.CancellationToken);

        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task PassesForAPrereleaseUsingTheUnreleasedEntry()
    {
        // Nothing writes a versioned heading before it ships, so a 0.x release reads [Unreleased].
        SetChangelog(
            """
            # Changelog

            ## [Unreleased]

            - A change.

            [Unreleased]: https://github.com/example/repo/commits/HEAD
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task FailsForAPrereleaseWithoutAnUnreleasedEntry()
    {
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("[Unreleased]");
    }

    [Fact]
    public async Task ProducesTheParsedChangelogForThePublishSteps()
    {
        // The GitHub release renders its notes from the entry in the produced changelog.
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0
            """);

        var result = await Step().Run(Project("1.2.0"), Releasable, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull()
            .Entry(NuGetVersion.Parse("1.2.0")).ShouldNotBeNull()
            .Body.ShouldContain("A change.");
    }

    [Fact]
    public async Task ProducesTheUnreleasedEntryForAPrerelease()
    {
        SetChangelog(
            """
            # Changelog

            ## [Unreleased]

            - An unreleased change.

            [Unreleased]: https://github.com/example/repo/commits/HEAD
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull()
            .Unreleased.ShouldNotBeNull()
            .Body.ShouldContain("An unreleased change.");
    }

    [Fact]
    public async Task FailsWhenTheEntryIsEmpty()
    {
        // An empty entry would ship a release with empty notes.
        SetChangelog(
            """
            # Changelog

            ## [Unreleased]

            [Unreleased]: https://github.com/example/repo/commits/HEAD
            """);

        var result = await Step().Run(Project("1.0.0-beta.1"), Releasable, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("empty");
    }

    [Fact]
    public async Task TreatsAnUnlabelledZeroPointVersionAsARelease()
    {
        // 0.0.1 has no prerelease label, so a feed serves it as the latest stable version and
        // people get it without asking for prereleases. It earns its own entry like any release.
        SetChangelog(
            """
            # Changelog

            ## [Unreleased]

            - A change.

            [Unreleased]: https://github.com/example/repo/commits/HEAD
            """);

        var result = await Step().Run(Project("0.0.1"), Releasable, TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("0.0.1");
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private void SetChangelog(string content)
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        _fileSystem.ProjectRoot.GetFile(_options.File).Returns(file);
    }

    private ChangelogValidate Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(_options), Options.Create(TestOptions.Git()), _fileSystem, _report, Changelogs);
}
