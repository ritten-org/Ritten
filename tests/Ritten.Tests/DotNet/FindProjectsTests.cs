using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Engine.FileSystem;
using Ritten.Reporting;

namespace Ritten.Tests.DotNet;

/// <summary>
/// What a repository builds, read off the disk for the jobs that run before anything says.
/// </summary>
public class FindProjectsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-projects-{Guid.NewGuid():N}");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public FindProjectsTests()
    {
        var root = new PhysicalDirectory(_root);
        _fileSystem.ProjectRoot.Returns(root);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public void SeparatesWhatShipsFromWhatTests()
    {
        Project("src/My.Tool/My.Tool.csproj");
        Project("src/My.Core/My.Core.csproj");
        Project("tests/My.Tool.Tests/My.Tool.Tests.csproj");

        var found = Run();

        found.Shipped.ShouldBe(["src/My.Core/My.Core.csproj", "src/My.Tool/My.Tool.csproj"]);
        found.Tests.ShouldBe(["tests/My.Tool.Tests/My.Tool.Tests.csproj"]);
    }

    [Fact]
    public void IgnoresTheBuildOutputsCopies()
    {
        Project("src/My.Tool/My.Tool.csproj");
        Project("src/My.Tool/bin/Debug/net10.0/My.Tool.csproj");
        Project("src/My.Tool/obj/My.Tool.csproj");

        Run().Shipped.ShouldHaveSingleItem().ShouldBe("src/My.Tool/My.Tool.csproj");
    }

    [Fact]
    public void FindsNothingInAnEmptyRepository()
    {
        Directory.CreateDirectory(_root);

        var found = Run();

        found.Shipped.ShouldBeEmpty();
        found.Tests.ShouldBeEmpty();
    }

    private DiscoveredProjects Run() =>
        new FindProjects(Substitute.For<IWorkflowLog>(), _fileSystem).Run().Value.ShouldNotBeNull();

    private void Project(string path)
    {
        var file = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }
}
