using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.Core.Settings;
using Ritten.Extensions;
using Ritten.Pipelines;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The links are a deterministic function of the file's own entries, so this lint applies in
/// every release state: drift is fixed in the pull request that caused it, not discovered by
/// whoever cuts the next release.
/// </summary>
public class ChangelogLinksValidateTests
{
    // The real client, so these tests exercise the actual link generator.
    private static readonly IChangelog Changelogs = new ServiceCollection()
        .AddChangelogs(new ChangelogSettings())
        .BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly ChangelogOptions _options = TestOptions.Changelog();

    public ChangelogLinksValidateTests()
    {
        _options.RepositoryUrl = "https://github.com/example/repo";
        _report.Section("Release").Returns(_releaseSection);
    }

    [Fact]
    public void PassesWhenTheLinksAreCorrect()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0
            """);

        var result = Step().Run(changelog);

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void FailsWithThePasteableBlockWhenTheLinksAreStale()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.0.0
            """);

        var result = Step().Run(changelog);

        result.IsFailure.ShouldBeTrue();
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        var failure = _releaseSection.Entries.OfType<ReportParagraph>().Last();
        failure.Markdown.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");
    }

    [Fact]
    public void OffersTheSameBlockToTheTerminalAsVerbatimContent()
    {
        // The terminal indents everything a step says, and a pasted leading space fails the very
        // check that printed the block — so it travels as verbatim content, rendered at the margin.
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.0.0
            """);

        var result = Step().Run(changelog);

        var error = result.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        var block = error.Verbatim.ShouldNotBeNull();
        block.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");
        block.Split('\n').ShouldAllBe(line => line == line.TrimStart());
    }

    [Fact]
    public void SkipsLinkValidationWithoutARepositoryUrl()
    {
        _options.RepositoryUrl = null;
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://example.com/completely-wrong
            """);

        var result = Step().Run(changelog);

        result.IsFailure.ShouldBeFalse();
    }

    private ChangelogLinksValidate Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(_options), Options.Create(TestOptions.Git()), _report, Changelogs);
}
