using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;

namespace Ritten.Tests.Contracts;

/// <summary>
/// A path written into a project file, a workflow file, or a sentence is spelled with forward
/// slashes whichever platform read it off the disk.
/// </summary>
public class DirectoryExtensionsTests
{
    private static readonly IDirectory Root = new PhysicalDirectory(Path.Combine(Path.GetTempPath(), "repo"));

    [Fact]
    public void WritesAFilesPathRelativeToTheDirectory()
    {
        var file = new PhysicalFile(Path.Combine(Root.AbsolutePath, "src", "My.Tool", "My.Tool.csproj"));

        Root.RelativePath(file).ShouldBe("src/My.Tool/My.Tool.csproj");
    }

    [Fact]
    public void WritesADirectorysPathRelativeToAnother()
    {
        var nested = new PhysicalDirectory(Path.Combine(Root.AbsolutePath, "services", "api"));

        Root.RelativePath(nested).ShouldBe("services/api");
    }

    [Fact]
    public void ADirectoryRelativeToItselfIsHere()
    {
        Root.RelativePath(Root).ShouldBe(".");
    }
}
