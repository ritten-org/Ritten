using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Changelogs.Steps;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;
using Ritten.Workflows;

namespace Ritten.Tests.Changelogs;

/// <summary>
/// Exercises the real parser and renderer, so what these tests assert is the file that lands
/// on disk rather than a model shape.
/// </summary>
public class PrepareChangelogTests
{
    private static readonly IChangelog Changelogs = WorkflowRunBuilderHelpers.Create()
        .AddChangelogs(new ChangelogSettings())
        .Services.BuildServiceProvider()
        .GetRequiredService<IChangelog>();

    private const string Existing =
        """
        # Changelog

        All notable changes to this project will be documented in this file.

        ## [Unreleased]

        ### Added

        - **A new thing.** It does something.

        ## [1.2.0] - 2026-08-01

        ### Fixed

        - **An old thing.** It was broken.

        [Unreleased]: https://github.com/example/repo/compare/v1.2.0...HEAD
        [1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0
        """;

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ChangelogOptions _options = TestOptions.Changelog();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 8, 21, 9, 0, 0, TimeSpan.Zero));
    private MemoryStream _written = new();

    [Fact]
    public async Task RollsTheUnreleasedNotesIntoTheVersion()
    {
        var file = SetChangelog(Existing);

        var result = await Step().Run(Changelog(Existing), Project(), Prepared("1.3.0"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var written = Written();
        written.ShouldContain("## [1.3.0] - 2026-08-21");
        written.ShouldNotContain("## [Unreleased]");

        // The notes carry over verbatim — preparing a release must not reword anybody's prose.
        written.ShouldContain("- **A new thing.** It does something.");
        written.ShouldContain("## [1.2.0] - 2026-08-01");
        file.Received().OpenWrite();
    }

    [Fact]
    public async Task RewritesTheVersionLinksForTheNewEntry()
    {
        SetChangelog(Existing);

        await Step().Run(Changelog(Existing), Project(), Prepared("1.3.0"), TestContext.Current.CancellationToken);

        var written = Written();
        written.ShouldContain("[1.3.0]: https://github.com/example/repo/compare/v1.2.0...v1.3.0");
        written.ShouldContain("[1.2.0]: https://github.com/example/repo/releases/tag/v1.2.0");

        // Nothing is unreleased any more, so the Unreleased link goes with it.
        written.ShouldNotContain("[Unreleased]:");
    }

    [Fact]
    public async Task WritesNothingWhenTheChangelogAlreadySaysIt()
    {
        // Re-running after a successful prepare: the file already describes the version.
        var prepared = Existing
            .Replace("## [Unreleased]", "## [1.3.0] - 2026-08-21")
            .Replace("[Unreleased]: https://github.com/example/repo/compare/v1.2.0...HEAD", "[1.3.0]: https://github.com/example/repo/compare/v1.2.0...v1.3.0");
        var file = SetChangelog(prepared);

        var result = await Step().Run(Changelog(prepared), Project(), Prepared("1.3.0", bumped: false), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        file.DidNotReceive().OpenWrite();
    }

    [Fact]
    public async Task LeavesTheEntriesAloneWhenThereIsNothingUnreleased()
    {
        // Only the links are derived, so they're still brought up to date.
        var withoutUnreleased = Existing
            .Replace("## [Unreleased]\n\n### Added\n\n- **A new thing.** It does something.\n\n", "")
            .Replace("[Unreleased]: https://github.com/example/repo/compare/v1.2.0...HEAD\n", "");
        SetChangelog(withoutUnreleased);

        var result = await Step().Run(Changelog(withoutUnreleased), Project(), Prepared("1.2.0", bumped: false), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        Written().ShouldNotContain("1.3.0");
    }

    private static Changelog Changelog(string content) => Changelogs.Parse(content);

    private static Project Project() => new()
    {
        Name = "My.Package",
        Version = NuGetVersion.Parse("1.2.0"),
        Repository = "https://github.com/example/repo"
    };

    private static PreparedRelease Prepared(string version, bool bumped = true) =>
        new(NuGetVersion.Parse(version), bumped, "because");

    private IFile SetChangelog(string content)
    {
        _written = new MemoryStream();
        var file = Substitute.For<IFile>();
        file.Name.Returns(_options.File);
        file.Exists.Returns(true);
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        file.OpenWrite().Returns(_ => _written);
        _fileSystem.ProjectRoot.GetFile(_options.File).Returns(file);
        return file;
    }

    // ToArray survives the writer disposing the stream.
    private string Written() => Encoding.UTF8.GetString(_written.ToArray());

    private PrepareChangelog Step() => new(
        Substitute.For<IWorkflowLog>(),
        Options.Create(_options),
        Options.Create(TestOptions.Git()),
        _fileSystem,
        Changelogs,
        _time);
}
