using System.Text.Json;
using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Releases;

namespace Ritten.Tests.Core;

public class PipelineSettingsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-settings-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task Read_ReadsSections()
    {
        WriteRittenJson(_root, """
            {
                "build": { "project": "src/Thing/Thing.csproj", "configuration": "Debug" },
                "repository": "https://example.com/thing",
                "changelog": { "file": "HISTORY.md" },
                "release": { "tagPrefix": "release-", "feed": "https://example.com/index.json" }
            }
            """);

        var settings = await Read(_root);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Build.Configuration.ShouldBe("Debug");
        settings.Repository.ShouldBe("https://example.com/thing");
        settings.Changelog.File.ShouldBe("HISTORY.md");
        settings.Release.TagPrefix.ShouldBe("release-");
        settings.Release.Feed.ShouldBe("https://example.com/index.json");
    }

    [Fact]
    public async Task Read_AppliesDefaultsForOmittedSections()
    {
        WriteRittenJson(_root);

        var settings = await Read(_root);

        settings.Build.Project.ShouldBeNull();
        settings.Build.Configuration.ShouldBe("Release");
        settings.Repository.ShouldBeNull();
        settings.Changelog.File.ShouldBe("CHANGELOG.md");
        settings.Release.TagPrefix.ShouldBe("v");
        settings.Release.Feed.ShouldBe("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public async Task Read_AppliesDefaultsForKeysOmittedWithinASection()
    {
        WriteRittenJson(_root, """{ "build": { "project": "src/Thing/Thing.csproj" } }""");

        var settings = await Read(_root);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Build.Configuration.ShouldBe("Release");
    }

    [Fact]
    public async Task Read_ReadsEnumValuesAsCamelCaseStrings()
    {
        WriteRittenJson(_root, """{ "release": { "lines": "minor" } }""");

        var settings = await Read(_root);

        settings.Release.Lines.ShouldBe(ReleaseLine.Minor);
    }

    [Fact]
    public async Task Read_ReadsTheCoverageSection()
    {
        WriteRittenJson(_root, """{ "coverage": { "line": 80.5 } }""");

        var settings = await Read(_root);

        settings.Coverage.Line.ShouldBe(80.5m);
        settings.Coverage.Branch.ShouldBeNull();
    }

    [Fact]
    public async Task Read_AppliesCoverageDefaultsWithoutItsSection()
    {
        // Coverage is always on; the section only sets minimums.
        WriteRittenJson(_root);

        var settings = await Read(_root);

        settings.Coverage.Line.ShouldBeNull();
        settings.Coverage.Branch.ShouldBeNull();
    }

    [Fact]
    public async Task Read_RejectsAnUnrecognisedEnumValue()
    {
        WriteRittenJson(_root, """{ "release": { "lines": "patch" } }""");
        var project = await RittenProject.Resolve(_root);

        var settings = PipelineSettings.Read<DotNetToolSettings>(project.Value.ShouldNotBeNull());

        settings.IsError.ShouldBeTrue();
        settings.Errors.ShouldHaveSingleItem().Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task Read_AllowsCommentsAndTrailingCommas()
    {
        // It's a hand-edited file.
        WriteRittenJson(_root, """
            {
                // The package this project ships.
                "build": { "project": "src/Thing/Thing.csproj", },
            }
            """);

        var settings = await Read(_root);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
    }

    private static async Task<DotNetToolSettings> Read(string directory)
    {
        var project = await RittenProject.Resolve(directory);
        var settings = PipelineSettings.Read<DotNetToolSettings>(project.Value.ShouldNotBeNull());
        settings.IsError.ShouldBeFalse();
        return settings.Value;
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.FileName), content);
    }
}
