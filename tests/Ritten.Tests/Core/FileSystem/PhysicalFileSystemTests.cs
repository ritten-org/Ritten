using Ritten.Core.FileSystem;

namespace Ritten.Tests.Core.FileSystem;

public class PhysicalFileSystemTests
{
    [Fact]
    public void RootDirectory_IsTheGivenRoot()
    {
        // Arrange
        var directory = Directory.GetCurrentDirectory();

        // Act
        var fileSystem = new PhysicalFileSystem(directory);

        // Assert
        fileSystem.ProjectRoot.AbsolutePath.ShouldBe(directory);
    }

    [Fact]
    public void RootDirectory_IsAbsolute()
    {
        // Act
        var fileSystem = new PhysicalFileSystem(".");

        // Assert
        Path.IsPathRooted(fileSystem.ProjectRoot.AbsolutePath).ShouldBeTrue();
    }
}
