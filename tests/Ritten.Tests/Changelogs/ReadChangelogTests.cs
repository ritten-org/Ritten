using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Pipelines;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Changelogs;

public class ReadChangelogTests
{
    // The real client, so these tests exercise the actual parser.
    private static readonly IChangelog Changelogs = new ServiceCollection()
        .AddChangelogs(new ChangelogSettings())
        .BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly ChangelogOptions _options = TestOptions.Changelog();

    public ReadChangelogTests()
    {
        _report.Section("Release").Returns(_releaseSection);
    }

    [Fact]
    public async Task ProducesTheParsedChangelog()
    {
        SetChangelog(
            """
            # Changelog

            ## [1.2.0] - 2026-08-01

            - A change.

            [1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0
            """);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeFalse();
        result.Value.ShouldNotBeNull()
            .Entry(NuGetVersion.Parse("1.2.0")).ShouldNotBeNull()
            .Body.ShouldContain("A change.");
    }

    [Fact]
    public async Task FailsWhenTheChangelogFileDoesNotExist()
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(false);
        _fileSystem.ProjectRoot.GetFile(_options.File).Returns(file);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain(_options.File);
        _releaseSection.Tone.ShouldBe(ReportTone.Failure);
    }

    private void SetChangelog(string content)
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        _fileSystem.ProjectRoot.GetFile(_options.File).Returns(file);
    }

    private ReadChangelog Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(_options), _fileSystem, _report, Changelogs);
}
