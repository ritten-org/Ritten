using System.Text.Json;
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
    public void Read_BindsCamelCaseKeys()
    {
        // Arrange
        WriteRittenJson(_root, """{ "project": "src/Thing/Thing.csproj", "changelog": "HISTORY.md" }""");

        // Act
        var file = RittenProject.Read(_root);

        // Assert
        file.Project.ShouldBe("src/Thing/Thing.csproj");
        file.Changelog.ShouldBe("HISTORY.md");
    }

    [Fact]
    public void Read_AppliesDefaultsForOmittedKeys()
    {
        // Arrange
        WriteRittenJson(_root);

        // Act
        var file = RittenProject.Read(_root);

        // Assert
        file.Project.ShouldBeNull();
        file.Configuration.ShouldBe("Release");
        file.Changelog.ShouldBe("CHANGELOG.md");
        file.TagPrefix.ShouldBe("v");
        file.Feed.ShouldBe("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public void Read_RejectsAnUnrecognisedKey()
    {
        // Arrange — a typo must not be silently ignored, or it surfaces later as "project must be set".
        WriteRittenJson(_root, """{ "projct": "src/Thing/Thing.csproj" }""");

        // Act
        Action act = () => RittenProject.Read(_root);

        // Assert
        act.ShouldThrow<JsonException>().Message.ShouldContain("projct");
    }

    [Fact]
    public void Read_RejectsAPascalCaseKey()
    {
        // Arrange
        WriteRittenJson(_root, """{ "Project": "src/Thing/Thing.csproj" }""");

        // Act
        Action act = () => RittenProject.Read(_root);

        // Assert
        act.ShouldThrow<JsonException>();
    }

    [Fact]
    public void Read_AllowsCommentsAndTrailingCommas()
    {
        // Arrange — it's a hand-edited file.
        WriteRittenJson(_root, """
            {
                // The package this repository ships.
                "project": "src/Thing/Thing.csproj",
            }
            """);

        // Act
        var file = RittenProject.Read(_root);

        // Assert
        file.Project.ShouldBe("src/Thing/Thing.csproj");
    }

    [Fact]
    public void Read_ThrowsOnMalformedJson()
    {
        // Arrange
        WriteRittenJson(_root, "{ not json");

        // Act
        Action act = () => RittenProject.Read(_root);

        // Assert — PipelineHost turns this into a configuration error rather than a crash.
        act.ShouldThrow<JsonException>();
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.FileName), content);
    }
}
