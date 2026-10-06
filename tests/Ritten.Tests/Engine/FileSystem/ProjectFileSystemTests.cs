using Microsoft.Extensions.Options;
using Ritten.Engine;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Workflows;

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

    [Fact]
    public void CreateTempDirectory_IsNewEmptyAndOutsideTheProject()
    {
        var fileSystem = new ProjectFileSystem(Project("."), Options.Create(new WorkflowOptions()));

        var first = fileSystem.CreateTempDirectory("ritten-scratch-");
        var second = fileSystem.CreateTempDirectory("ritten-scratch-");
        try
        {
            first.Exists.ShouldBeTrue();
            first.Name.ShouldStartWith("ritten-scratch-");
            first.GetFiles("**/*").ShouldBeEmpty();
            second.AbsolutePath.ShouldNotBe(first.AbsolutePath);
            first.AbsolutePath.ShouldNotStartWith(fileSystem.ProjectRoot.AbsolutePath);
        }
        finally
        {
            first.Delete();
            second.Delete();
        }
    }

    private static RittenProject Project(string directory) => new()
    {
        Directory = directory
    };
}
