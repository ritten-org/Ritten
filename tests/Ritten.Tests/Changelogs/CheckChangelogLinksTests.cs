using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Changelogs.Steps;
using Ritten.DotNet;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;
using Ritten.Workflows;

namespace Ritten.Tests.Changelogs;

/// <summary>
/// The links are a deterministic function of the file's own entries, so this lint applies in
/// every release state. The repository URL arrives already resolved on the project.
/// </summary>
public class CheckChangelogLinksTests
{
    // The real client, so these tests exercise the actual link generator.
    private static readonly IChangelog Changelogs = WorkflowRunBuilderHelpers.Create()
        .AddChangelogs(new ChangelogSettings())
        .Services.BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _changelogSection = new("Changelog");
    private readonly ChangelogOptions _options = TestOptions.Changelog();

    public CheckChangelogLinksTests()
    {
        _report.Section("Changelog").Returns(_changelogSection);
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

        var result = Step().Run(Project(), changelog);

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

        var result = Step().Run(Project(), changelog);

        result.IsFailure.ShouldBeTrue();
        _changelogSection.Tone.ShouldBe(ReportTone.Failure);
        var failure = _changelogSection.Entries.OfType<ReportParagraph>().Last();
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

        var result = Step().Run(Project(), changelog);

        var error = result.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        var block = error.Verbatim.ShouldNotBeNull();
        block.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");
        block.Split('\n').ShouldAllBe(line => line == line.TrimStart());
    }

    [Fact]
    public void SkipsLinkCheckWhenTheProjectHasNoRepository()
    {
        var changelog = Changelogs.Parse(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://example.com/completely-wrong
            """);

        var result = Step().Run(Project(repository: null), changelog);

        result.IsFailure.ShouldBeFalse();
    }

    private static Project Project(string? repository = "https://github.com/example/repo") =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0"), Repository = repository };

    private CheckChangelogLinks Step() =>
        new(Substitute.For<IWorkflowLog>(), Options.Create(_options), Options.Create(TestOptions.Git()), _report, Changelogs);
}
