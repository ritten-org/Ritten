using Microsoft.Extensions.Options;
using Ritten.Engine;
using Ritten.Engine.FileSystem;

namespace Ritten.Tests.Engine.FileSystem;

public class ProjectFileSystemTests
{
    [Fact]
    public void ProjectRoot_IsTheProjectDirectory()
    {
        // Arrange
        var directory = Directory.GetCurrentDirectory();

        // Act
        var fileSystem = new ProjectFileSystem(Project(directory), Options.Create(new WorkflowOptions()));

        // Assert
        fileSystem.ProjectRoot.AbsolutePath.ShouldBe(directory);
    }

    [Fact]
    public void ProjectRoot_IsAbsolute()
    {
        // Act
        var fileSystem = new ProjectFileSystem(Project("."), Options.Create(new WorkflowOptions()));

        // Assert
        Path.IsPathRooted(fileSystem.ProjectRoot.AbsolutePath).ShouldBeTrue();
    }

    private static RittenProject Project(string directory) => new()
    {
        Directory = directory
    };
}
