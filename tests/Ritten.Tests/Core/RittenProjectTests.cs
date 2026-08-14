using Ritten.Core;

namespace Ritten.Tests.Core;

public class RittenProjectTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-repo-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public void Find_ReturnsTheDirectoryContainingTheFile()
    {
        // Arrange
        WriteRittenJson(_root);

        // Act
        var found = RittenProject.Find(_root);

        // Assert
        found.ShouldBe(_root);
    }

    [Fact]
    public void Find_WalksUpFromASubdirectory()
    {
        // Arrange
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "src", "Thing", "bin")).FullName;

        // Act
        var found = RittenProject.Find(nested);

        // Assert
        found.ShouldBe(_root);
    }

    [Fact]
    public void Find_PrefersTheNearestFile()
    {
        // Arrange — a nested repository shadows the outer one.
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "nested")).FullName;
        WriteRittenJson(nested);

        // Act
        var found = RittenProject.Find(Path.Combine(nested, "src"));

        // Assert
        found.ShouldBe(nested);
    }

    [Fact]
    public void Find_ReturnsNullWhenNoFileExistsUpToTheFilesystemRoot()
    {
        // Arrange
        Directory.CreateDirectory(_root);

        // Act
        var found = RittenProject.Find(_root);

        // Assert
        found.ShouldBeNull();
    }

    [Fact]
    public void ReadConfiguration_BindsTheFile()
    {
        // Arrange
        WriteRittenJson(_root, """{ "Changelog": { "File": "HISTORY.md" } }""");

        // Act
        var configuration = RittenProject.ReadConfiguration(_root);

        // Assert
        configuration["Changelog:File"].ShouldBe("HISTORY.md");
    }

    [Fact]
    public void ReadConfiguration_ThrowsOnMalformedJson()
    {
        // Arrange
        WriteRittenJson(_root, "{ not json");

        // Act
        var act = () => RittenProject.ReadConfiguration(_root);

        // Assert — RittenApplication turns this into a configuration error rather than a crash.
        act.ShouldThrow<Exception>();
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.FileName), content);
    }
}
