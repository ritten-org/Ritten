using System.Text;
using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

public class DotNetClientVersionTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _root = Substitute.For<IDirectory>();
    private readonly Dictionary<string, MemoryStream> _written = [];
    private readonly DotNetClient _client;

    public DotNetClientVersionTests()
    {
        _fileSystem.ProjectRoot.Returns(_root);
        _client = new DotNetClient(_commands, _fileSystem);
    }

    [Fact]
    public async Task WritesTheVersionWhereverItIsDeclared()
    {
        SetFile("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <Version>1.2.0</Version>\n  </PropertyGroup>\n</Project>\n");
        SetFile("src/Thing/Thing.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <PackageId>Thing</PackageId>\n  </PropertyGroup>\n</Project>\n");

        var result = await Set(["src/Thing/Thing.csproj"], "1.2.0", "1.3.0");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(["Directory.Build.props"]);
        Written("Directory.Build.props").ShouldContain("<Version>1.3.0</Version>");
    }

    [Fact]
    public async Task WritesEveryFileThatDeclaresIt()
    {
        // Per-project versions: lockstep means every declaration moves together.
        SetFile("src/A/A.csproj", "<Project>\n  <Version>1.2.0</Version>\n</Project>\n");
        SetFile("src/B/B.csproj", "<Project>\n  <Version>1.2.0</Version>\n</Project>\n");

        var result = await Set(["src/A/A.csproj", "src/B/B.csproj"], "1.2.0", "1.3.0");

        result.Value.ShouldBe(["src/A/A.csproj", "src/B/B.csproj"]);
        Written("src/A/A.csproj").ShouldContain("<Version>1.3.0</Version>");
        Written("src/B/B.csproj").ShouldContain("<Version>1.3.0</Version>");
    }

    [Fact]
    public async Task LeavesEverythingElseInTheFileExactlyAsItWas()
    {
        // Round-tripping the XML would reformat a file the caller has to read.
        const string Original = "<Project>\r\n\t<PropertyGroup>\r\n\t\t<Version>1.2.0</Version>\r\n\t\t<Other>  spaced  </Other>\r\n\t</PropertyGroup>\r\n</Project>\r\n";
        SetFile("Directory.Build.props", Original);

        await Set(["src/Thing/Thing.csproj"], "1.2.0", "1.3.0");

        Written("Directory.Build.props").ShouldBe(Original.Replace("<Version>1.2.0</Version>", "<Version>1.3.0</Version>"));
    }

    [Fact]
    public async Task RefusesWhenNothingDeclaresTheVersion()
    {
        // The version is computed from somewhere Ritten can't see, and rewriting a guess would
        // be worse than saying so.
        SetFile("Directory.Build.props", "<Project>\n  <Version>$(BuildVersion)</Version>\n</Project>\n");

        var result = await Set(["src/Thing/Thing.csproj"], "1.2.0", "1.3.0");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("<Version>1.2.0</Version>");
        _written.ShouldNotContainKey("Directory.Build.props");
    }

    private Task<Ritten.Engine.Result<IReadOnlyList<string>>> Set(string[] projects, string current, string version) =>
        _client.SetVersion(
            new SetVersionArgs
            {
                Projects = projects,
                Current = NuGetVersion.Parse(current),
                Version = NuGetVersion.Parse(version)
            },
            TestContext.Current.CancellationToken);

    private void SetFile(string path, string content)
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        file.Name.Returns(Path.GetFileName(path));
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        file.OpenWrite().Returns(_ =>
        {
            var stream = new MemoryStream();
            _written[path] = stream;
            return stream;
        });
        _root.GetFile(path).Returns(file);
    }

    private string Written(string path) => Encoding.UTF8.GetString(_written[path].ToArray());
}
