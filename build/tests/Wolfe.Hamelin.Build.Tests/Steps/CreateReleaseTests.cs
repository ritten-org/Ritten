using Hamelin;
using Hamelin.FileSystem;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Steps;
using Wolfe.Hamelin.Build.Tests.Support;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Tests.Steps;

public class CreateReleaseTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IPipelineContext _context = Substitute.For<IPipelineContext>();
    private readonly IChangelog _changelogs = Substitute.For<IChangelog>();
    private readonly IFile _notesFile = Substitute.For<IFile>();
    private readonly ChangelogEntry _entry = new() { Version = NuGetVersion.Parse("1.2.0"), Added = ["A thing."] };

    public CreateReleaseTests()
    {
        SetVersion("1.2.0");
        _context.State.Get<ChangelogEntry>(Arg.Any<string>()).Returns(_entry);

        _notesFile.AbsolutePath.Returns("/repo/temp/release-notes-1.2.0.md");
        _context.FileSystem.CurrentDirectory.GetDirectory("temp").GetFile("release-notes-1.2.0.md").Returns(_notesFile);
    }

    [Fact]
    public async Task SkipsPrereleaseVersions()
    {
        SetVersion("1.2.0-beta.1");

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.Executed.ShouldBeEmpty();
    }

    [Fact]
    public async Task SkipsWhenTheReleaseAlreadyExists()
    {
        _commands.Respond(c => c.Arguments.Contains("view"), new CommandResult(0, "{\"name\":\"v1.2.0\"}", ""));

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.Executed.Any(c => c.Arguments.Contains("create")).ShouldBeFalse();
        await _changelogs.DidNotReceiveWithAnyArgs().WriteEntry(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesTheReleaseWithTheChangelogEntryAsNotes()
    {
        _commands.Respond(c => c.Arguments.Contains("view"), new CommandResult(1, "", "release not found"));

        await Step().Run(TestContext.Current.CancellationToken);

        await _changelogs.Received().WriteEntry(_notesFile, _entry, Arg.Any<CancellationToken>());
        var create = _commands.Executed.Single(c => c.Arguments.Contains("create"));
        create.Path.ShouldBe("gh");
        create.Arguments.ShouldBe(["release", "create", "v1.2.0", "--title", "v1.2.0", "--notes-file", "/repo/temp/release-notes-1.2.0.md"]);
        create.ThrowsOnError.ShouldBeTrue();
    }

    private void SetVersion(string version) =>
        _context.State.Get<Project>(Arg.Any<string>())
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse(version) });

    private CreateRelease Step() =>
        new(NullLogger<CreateRelease>.Instance, Options.Create(TestOptions.Build()), Options.Create(TestOptions.Release()), _context, _commands, _changelogs);
}
