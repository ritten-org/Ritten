using System.Text;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Extensions;
using Ritten.Pipelines;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class ValidateChangelogTests
{
    // The real client, so these tests exercise the actual parser and link generator.
    private static readonly IChangelog Changelogs = new ServiceCollection()
        .AddChangelogs()
        .BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IPipelineState _state = Substitute.For<IPipelineState>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly ChangelogOptions _options = TestOptions.Changelog();

    public ValidateChangelogTests()
    {
        _options.RepositoryUrl = "https://github.com/example/repo";
        _state.Get<Project>()
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") });
        _report.Section("Release").Returns(_releaseSection);
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

        await Step().Run(TestContext.Current.CancellationToken);

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

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
        var failure = _releaseSection.Entries.OfType<ReportParagraph>().Last();
        failure.Markdown.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");
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

        await Step().Run(TestContext.Current.CancellationToken);

        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    private void SetChangelog(string content)
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        _fileSystem.CurrentDirectory.GetFile(_options.File).Returns(file);
    }

    private ValidateChangelog Step() =>
        new(NullLogger<ValidateChangelog>.Instance, Options.Create(_options), Options.Create(TestOptions.Git()), _fileSystem, _state, _report, Changelogs);
}
