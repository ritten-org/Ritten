using Microsoft.Extensions.DependencyInjection;
using Ritten.Changelogs;
using Ritten.Contracts.FileSystem;
using Ritten.Init.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Init;

public class EnsureChangelogTests
{
    private static readonly IChangelog Changelogs = new TestWorkflowBuilder()
        .AddChangelogs()
        .Services.BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ChangelogSettings _options = TestOptions.Changelog();
    private MemoryFile _changelog = MemoryFile.Missing("CHANGELOG.md");

    [Fact]
    public async Task WritesAChangelogWhenThereIsNone()
    {
        SetChangelog(exists: false);

        var result = await Step().Run(_options, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var written = Written();
        written.ShouldContain("# Changelog");
        written.ShouldContain("Keep a Changelog");
        written.ShouldContain("## [Unreleased]");
    }

    [Fact]
    public async Task GivesAChangelogSomewhereToWriteTheNextRelease()
    {
        SetChangelog(exists: true, content:
            """
            # Changelog

            ## [1.0.0] - 2026-01-01

            ### Added

            - **A thing.** It does something.
            """);

        await Step().Run(_options, TestContext.Current.CancellationToken);

        // The unreleased notes go above everything already shipped, and nobody's prose is touched.
        var written = Written();
        written.IndexOf("## [Unreleased]", StringComparison.Ordinal).ShouldBeLessThan(written.IndexOf("## [1.0.0]", StringComparison.Ordinal));
        written.ShouldContain("- **A thing.** It does something.");
    }

    [Fact]
    public async Task LeavesAChangelogThatAlreadyHasOne()
    {
        var file = SetChangelog(exists: true, content: "# Changelog\n\n## [Unreleased]\n");

        var result = await Step().Run(_options, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        file.Writes.ShouldBe(0);
    }

    private EnsureChangelog Step() =>
        new(Substitute.For<IWorkflowLog>(), _fileSystem, Changelogs);

    private string Written() => _changelog.Text.ShouldNotBeNull();

    private MemoryFile SetChangelog(bool exists, string content = "")
    {
        _changelog = exists ? MemoryFile.Existing(_options.File, content) : MemoryFile.Missing(_options.File);
        _fileSystem.ProjectRoot.GetFile(_options.File).Returns(_changelog);
        return _changelog;
    }
}
