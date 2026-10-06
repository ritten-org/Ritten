using Ritten.Engine.FileSystem;

namespace Ritten.Tests.Engine.FileSystem;

public class PhysicalDirectoryTests
{
    [Fact]
    public void Exists_DirectoryDoesNotExist_ShouldBeFalse()
    {
        // Arrange
        var path = "./foobar";

        // Act
        var dir = new PhysicalDirectory(path);

        // Assert
        dir.Exists.ShouldBeFalse();
    }

    [Fact]
    public void Exists_DirectoryExists_ShouldBeTrue()
    {
        // Arrange
        var path = "Engine/FileSystem";

        // Act
        var dir = new PhysicalDirectory(path);

        // Assert
        dir.Exists.ShouldBeTrue();
    }

    [Fact]
    public void Name_ShouldBeFileName()
    {
        // Arrange
        var path = "Engine/FileSystem";

        // Act
        var dir = new PhysicalDirectory(path);

        // Assert
        dir.Name.ShouldBe("FileSystem");
    }

    [Fact]
    public void Create_ExistingDirectory_DoesNotThrow()
    {
        // Arrange
        var path = "Engine/FileSystem";
        var dir = new PhysicalDirectory(path);
        dir.Exists.ShouldBeTrue();

        // Act
        var act = () => dir.Create();

        // Assert
        act.ShouldNotThrow();
    }

    [Fact]
    public void Create_NewDirectory_CreatesDirectory()
    {
        // Arrange
        var path = "Engine/FileSystem";
        var dir = new PhysicalDirectory(path).GetDirectory("create-test");
        dir.Exists.ShouldBeFalse();

        try
        {
            // Act
            dir.Create();

            // Assert
            dir.Exists.ShouldBeTrue();
        }
        finally
        {
            dir.Delete();
        }
    }

    [Fact]
    public void Delete_NonExistentDirectory_DoesNotThrow()
    {
        // Arrange
        var path = "Engine/FileSystem";
        var dir = new PhysicalDirectory(path).GetDirectory("does-not-exist");
        dir.Exists.ShouldBeFalse();

        // Act
        var act = () => dir.Delete();

        // Assert
        act.ShouldNotThrow();
    }

    [Fact]
    public void Delete_ExistingDirectory_DeletesDirectory()
    {
        // Arrange
        var path = "Engine/FileSystem";
        var dir = new PhysicalDirectory(path).GetDirectory("delete-test");
        dir.Create();
        dir.Exists.ShouldBeTrue();

        // Act
        dir.Delete();

        // Assert
        dir.Exists.ShouldBeFalse();
    }

    [Fact]
    public void GetDirectory_ExistingDirectory_GetsDirectory()
    {
        // Arrange
        var path = "./";
        var directory = "Engine";
        var dir = new PhysicalDirectory(path);

        // Act
        var subDir = dir.GetDirectory(directory);

        // Assert
        subDir.Exists.ShouldBeTrue();
        subDir.Name.ShouldBe("Engine");
    }

    [Fact]
    public void GetDirectory_MissingDirectory_StillGetsDirectory()
    {
        // Arrange
        var path = "./";
        var directory = "DoesNotExist";
        var dir = new PhysicalDirectory(path);

        // Act
        var subDir = dir.GetDirectory(directory);

        // Assert
        subDir.Exists.ShouldBeFalse();
        subDir.Name.ShouldBe("DoesNotExist");
    }

    [Fact]
    public void GetDirectories_ShouldContainKnownDirectory()
    {
        // Arrange
        var path = "./";
        var dir = new PhysicalDirectory(path);

        // Act
        var directories = dir.GetDirectories();

        // Assert
        directories.ShouldContain(d => d.Name == "Engine");
    }

    [Fact]
    public void GetFiles_AllFiles_ShouldContainKnownFile()
    {
        // Arrange
        var path = "./";
        var dir = new PhysicalDirectory(path);

        // Act
        var files = dir.GetFiles();

        // Assert
        files.ShouldContain(d => d.Name == "Ritten.dll");
    }

    [Fact]
    public void GetFiles_AllFiles_ShouldNotContainFilesInSubdirectories()
    {
        // Arrange
        var path = "./";
        var dir = new PhysicalDirectory(path);

        // Act
        var files = dir.GetFiles();

        // Assert
        files.ShouldNotContain(d => d.Name == "TestFile.txt");
    }

    [Fact]
    public void GetFiles_Search_ShouldReturnMatchingFile()
    {
        // Arrange
        var path = "./";
        var dir = new PhysicalDirectory(path);

        // Act
        var files = dir.GetFiles("**/*.txt");

        // Assert
        files.ShouldContain(d => d.Name == "TestFile.txt");
    }

    [Fact]
    public void GetFiles_EveryFileBeneath_IncludesHiddenOnes()
    {
        using var root = NewScratch();
        File.WriteAllText(Path.Combine(root.FullName, ".hidden"), "");
        Directory.CreateDirectory(Path.Combine(root.FullName, ".config", "nested"));
        File.WriteAllText(Path.Combine(root.FullName, ".config", "nested", "settings.json"), "");

        var files = new PhysicalDirectory(root.FullName).GetFiles("**/*").Select(file => file.Name);

        files.ShouldBe([".hidden", "settings.json"], ignoreOrder: true);
    }

    [Fact]
    public void GetDirectories_Recursive_ReturnsEveryDirectoryBeneath_HiddenOnesIncluded()
    {
        using var root = NewScratch();
        Directory.CreateDirectory(Path.Combine(root.FullName, ".config", "nested"));
        Directory.CreateDirectory(Path.Combine(root.FullName, "plain"));

        var directory = new PhysicalDirectory(root.FullName);

        directory.GetDirectories().Select(d => d.Name).ShouldBe([".config", "plain"], ignoreOrder: true);
        directory.GetDirectories(recursive: true).Select(d => d.Name).ShouldBe([".config", "nested", "plain"], ignoreOrder: true);
    }

    [Fact]
    public void LastWriteTime_IsWhenAnEntryLastCameOrWent()
    {
        using var root = NewScratch();
        var written = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        Directory.SetLastWriteTimeUtc(root.FullName, written);

        new PhysicalDirectory(root.FullName).LastWriteTime.ShouldBe(new DateTimeOffset(written));
    }

    [Fact]
    public void MoveTo_TakesTheContentsAlong()
    {
        using var root = NewScratch();
        var staging = Directory.CreateDirectory(Path.Combine(root.FullName, "staging"));
        File.WriteAllText(Path.Combine(staging.FullName, "tool"), "binary");
        var destination = new PhysicalDirectory(Path.Combine(root.FullName, "1.0.0"));

        new PhysicalDirectory(staging.FullName).MoveTo(destination);

        staging.Exists.ShouldBeFalse();
        destination.GetFile("tool").Exists.ShouldBeTrue();
    }

    private static Scratch NewScratch() => new(Directory.CreateTempSubdirectory("ritten-directory-"));

    private sealed class Scratch(DirectoryInfo directory) : IDisposable
    {
        public string FullName => directory.FullName;

        public void Dispose() => directory.Delete(recursive: true);
    }
}
