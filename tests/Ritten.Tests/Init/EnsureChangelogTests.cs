using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ritten.Changelogs;
using Ritten.Contracts.FileSystem;
using Ritten.Init.Steps;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;
using Ritten.Workflows;

namespace Ritten.Tests.Init;

public class EnsureChangelogTests
{
    private static readonly IChangelog Changelogs = WorkflowRunBuilderHelpers.Create()
        .AddChangelogs(new ChangelogSettings())
        .Services.BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ChangelogOptions _options = TestOptions.Changelog();
    private MemoryStream _written = new();

    [Fact]
    public async Task WritesAChangelogWhenThereIsNone()
    {
        SetChangelog(exists: false);

        var result = await Step().Run(TestContext.Current.CancellationToken);

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

        await Step().Run(TestContext.Current.CancellationToken);

        // The unreleased notes go above everything already shipped, and nobody's prose is touched.
        var written = Written();
        written.IndexOf("## [Unreleased]", StringComparison.Ordinal).ShouldBeLessThan(written.IndexOf("## [1.0.0]", StringComparison.Ordinal));
        written.ShouldContain("- **A thing.** It does something.");
    }

    [Fact]
    public async Task LeavesAChangelogThatAlreadyHasOne()
    {
        var file = SetChangelog(exists: true, content: "# Changelog\n\n## [Unreleased]\n");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        file.DidNotReceive().OpenWrite();
    }

    private EnsureChangelog Step() =>
        new(Substitute.For<IWorkflowLog>(), Microsoft.Extensions.Options.Options.Create(_options), _fileSystem, Changelogs);

    private string Written() => Encoding.UTF8.GetString(_written.ToArray());

    private IFile SetChangelog(bool exists, string content = "")
    {
        _written = new MemoryStream();
        var file = Substitute.For<IFile>();
        file.Name.Returns(_options.File);
        file.Exists.Returns(exists);
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        file.OpenWrite().Returns(_ => _written);
        _fileSystem.ProjectRoot.GetFile(_options.File).Returns(file);
        return file;
    }
}
