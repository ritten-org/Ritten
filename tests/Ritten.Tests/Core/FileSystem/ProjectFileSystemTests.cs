using Ritten.Core.FileSystem;

namespace Ritten.Tests.Core.FileSystem;

public class ProjectFileSystemTests
{
    [Fact]
    public void RootDirectory_IsTheGivenRoot()
    {
        // Arrange
        var directory = Directory.GetCurrentDirectory();

        // Act
        var fileSystem = new ProjectFileSystem(directory);

        // Assert
        fileSystem.ProjectRoot.AbsolutePath.ShouldBe(directory);
    }

    [Fact]
    public void RootDirectory_IsAbsolute()
    {
        // Act
        var fileSystem = new ProjectFileSystem(".");

        // Assert
        Path.IsPathRooted(fileSystem.ProjectRoot.AbsolutePath).ShouldBeTrue();
    }
}
