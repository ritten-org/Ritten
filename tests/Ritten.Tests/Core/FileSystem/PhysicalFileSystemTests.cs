using Ritten.Core.FileSystem;

namespace Ritten.Tests.Core.FileSystem;

public class PhysicalFileSystemTests
{
    [Fact]
    public void CurrentDirectory_IsCurrentDirectory()
    {
        // Arrange
        var directory = Directory.GetCurrentDirectory();

        // Act
        var fileSystem = new PhysicalFileSystem(directory);

        // Assert
        fileSystem.CurrentDirectory.AbsolutePath.ShouldBe(directory);
    }

    [Fact]
    public void RootDirectory_IsRootDirectory()
    {
        // Arrange
        var directory = Directory.GetCurrentDirectory();
        var root = Path.GetPathRoot(directory);

        // Act
        var fileSystem = new PhysicalFileSystem(directory);

        // Assert
        fileSystem.RootDirectory.AbsolutePath.ShouldBe(root);
    }
}
