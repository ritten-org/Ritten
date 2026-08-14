using Ritten.Core;
using Ritten.Core.FileSystem;

namespace Ritten.Tests.Core.FileSystem;

public class ProjectFileSystemTests
{
    [Fact]
    public void ProjectRoot_IsTheProjectDirectory()
    {
        // Arrange
        var directory = Directory.GetCurrentDirectory();

        // Act
        var fileSystem = new ProjectFileSystem(Project(directory));

        // Assert
        fileSystem.ProjectRoot.AbsolutePath.ShouldBe(directory);
    }

    [Fact]
    public void ProjectRoot_IsAbsolute()
    {
        // Act
        var fileSystem = new ProjectFileSystem(Project("."));

        // Assert
        Path.IsPathRooted(fileSystem.ProjectRoot.AbsolutePath).ShouldBeTrue();
    }

    private static RittenProject Project(string directory) => new()
    {
        Directory = directory
    };
}
