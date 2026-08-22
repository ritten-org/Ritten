using System.Text;
using Ritten.Contracts.FileSystem;
using Ritten.Init;

namespace Ritten.Tests.Init;

public class ScaffolderTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _root = Substitute.For<IDirectory>();

    public ScaffolderTests() => _fileSystem.ProjectRoot.Returns(_root);

    [Fact]
    public async Task WritesWhatIsMissing()
    {
        var written = SetFile("ritten.json", exists: false);

        var outcomes = await Apply(new ScaffoldedFile("ritten.json", "{}\n"));

        outcomes.ShouldHaveSingleItem().Outcome.ShouldBe(ScaffoldOutcome.Written);
        Encoding.UTF8.GetString(written.ToArray()).ShouldBe("{}\n");
    }

    [Fact]
    public async Task NeverOverwritesWhatIsAlreadyThere()
    {
        // A repository's files are its own; silently replacing an edited one would be the worst
        // possible way to find that out.
        var written = SetFile("ritten.json", exists: true, content: "{ \"workflow\": \"mine\" }");

        var outcomes = await Apply(new ScaffoldedFile("ritten.json", "{}\n"));

        outcomes.ShouldHaveSingleItem().Outcome.ShouldBe(ScaffoldOutcome.Matches);
        written.ToArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task ChecksGeneratedFilesForDrift()
    {
        SetFile("ritten.yml", exists: true, content: "hand edited");

        var outcomes = await Apply(new ScaffoldedFile("ritten.yml", "generated", Generated: true));

        outcomes.ShouldHaveSingleItem().Outcome.ShouldBe(ScaffoldOutcome.Differs);
    }

    [Fact]
    public async Task LeavesSeedsAloneEvenWhenTheyDiffer()
    {
        // A changelog diverges the moment anybody writes an entry, so checking it would report
        // drift on every repository, immediately.
        SetFile("CHANGELOG.md", exists: true, content: "## [1.0.0]\n\nReal entries.\n");

        var outcomes = await Apply(new ScaffoldedFile("CHANGELOG.md", "# Changelog\n"));

        outcomes.ShouldHaveSingleItem().Outcome.ShouldBe(ScaffoldOutcome.Matches);
    }

    [Fact]
    public async Task WritesNothingWhenChecking()
    {
        var written = SetFile("ritten.json", exists: false);

        var outcomes = await Apply(new ScaffoldedFile("ritten.json", "{}\n"), check: true);

        outcomes.ShouldHaveSingleItem().Outcome.ShouldBe(ScaffoldOutcome.Written);
        written.ToArray().ShouldBeEmpty();
    }

    private MemoryStream SetFile(string path, bool exists, string content = "")
    {
        var written = new MemoryStream();
        var file = Substitute.For<IFile>();
        file.Exists.Returns(exists);
        file.AbsolutePath.Returns($"/repo/{path}");
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        file.OpenWrite().Returns(_ => written);
        _root.GetFile(path).Returns(file);
        return written;
    }

    private async Task<IReadOnlyList<(ScaffoldedFile File, ScaffoldOutcome Outcome)>> Apply(ScaffoldedFile file, bool check = false) =>
        await new Scaffolder(_fileSystem).Apply([file], _root, check, TestContext.Current.CancellationToken);
}
