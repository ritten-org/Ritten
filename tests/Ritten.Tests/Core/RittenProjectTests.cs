using System.Text.Json;
using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Releases;

namespace Ritten.Tests.Core;

public class RittenProjectTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-project-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task Resolve_ReturnsTheDirectoryContainingTheFile()
    {
        WriteRittenJson(_root);

        var project = await RittenProject.Resolve(_root);

        project.Value.ShouldNotBeNull().Directory.ShouldBe(_root);
    }

    [Fact]
    public async Task Resolve_WalksUpFromASubdirectory()
    {
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "src", "Thing", "bin")).FullName;

        var project = await RittenProject.Resolve(nested);

        project.Value.ShouldNotBeNull().Directory.ShouldBe(_root);
    }

    [Fact]
    public async Task Resolve_PrefersTheNearestFile()
    {
        // A nested project shadows the outer one.
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "nested")).FullName;
        WriteRittenJson(nested);

        var project = await RittenProject.Resolve(Path.Combine(nested, "src"));

        project.Value.ShouldNotBeNull().Directory.ShouldBe(nested);
    }

    [Fact]
    public async Task Resolve_ReportsNoProjectUpToTheFilesystemRoot()
    {
        Directory.CreateDirectory(_root);

        var project = await RittenProject.Resolve(_root);

        project.IsError.ShouldBeTrue();
        project.Errors.ShouldHaveSingleItem().Message.ShouldContain("No ritten.json found");
    }

    [Fact]
    public async Task Resolve_ReportsMalformedJsonWithoutThrowing()
    {
        WriteRittenJson(_root, "{ not json");

        var project = await RittenProject.Resolve(_root);

        project.IsError.ShouldBeTrue();
        var error = project.Errors.ShouldHaveSingleItem();
        error.Message.ShouldContain("Could not read");
        error.Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task GetSettings_ReadsSections()
    {
        WriteRittenJson(_root, """
            {
                "build": { "project": "src/Thing/Thing.csproj", "configuration": "Debug" },
                "changelog": { "file": "HISTORY.md", "repository": "https://example.com/thing" },
                "release": { "tagPrefix": "release-", "feed": "https://example.com/index.json" }
            }
            """);

        var settings = await GetSettings(_root);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Build.Configuration.ShouldBe("Debug");
        settings.Changelog.File.ShouldBe("HISTORY.md");
        settings.Changelog.Repository.ShouldBe("https://example.com/thing");
        settings.Release.TagPrefix.ShouldBe("release-");
        settings.Release.Feed.ShouldBe("https://example.com/index.json");
    }

    [Fact]
    public async Task GetSettings_AppliesDefaultsForOmittedSections()
    {
        WriteRittenJson(_root);

        var settings = await GetSettings(_root);

        settings.Build.Project.ShouldBeNull();
        settings.Build.Configuration.ShouldBe("Release");
        settings.Changelog.File.ShouldBe("CHANGELOG.md");
        settings.Changelog.Repository.ShouldBeNull();
        settings.Release.TagPrefix.ShouldBe("v");
        settings.Release.Feed.ShouldBe("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public async Task GetSettings_AppliesDefaultsForKeysOmittedWithinASection()
    {
        WriteRittenJson(_root, """{ "build": { "project": "src/Thing/Thing.csproj" } }""");

        var settings = await GetSettings(_root);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Build.Configuration.ShouldBe("Release");
    }

    [Fact]
    public async Task GetSettings_ReadsEnumValuesAsCamelCaseStrings()
    {
        WriteRittenJson(_root, """{ "release": { "lines": "minor" } }""");

        var settings = await GetSettings(_root);

        settings.Release.Lines.ShouldBe(ReleaseLine.Minor);
    }

    [Fact]
    public async Task GetSettings_ReadsTheCoverageSection()
    {
        WriteRittenJson(_root, """{ "coverage": { "line": 80.5 } }""");

        var settings = await GetSettings(_root);

        settings.Coverage.ShouldNotBeNull().Line.ShouldBe(80.5m);
        settings.Coverage.Branch.ShouldBeNull();
    }

    [Fact]
    public async Task GetSettings_LeavesCoverageOffWithoutItsSection()
    {
        WriteRittenJson(_root);

        var settings = await GetSettings(_root);

        settings.Coverage.ShouldBeNull();
    }

    [Fact]
    public async Task GetSettings_RejectsAnUnrecognisedEnumValue()
    {
        WriteRittenJson(_root, """{ "release": { "lines": "patch" } }""");
        var project = await RittenProject.Resolve(_root);

        var settings = project.Value.ShouldNotBeNull().GetSettings<DotNetToolSettings>();

        settings.IsError.ShouldBeTrue();
        settings.Errors.ShouldHaveSingleItem().Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task GetSettings_RejectsAnUnrecognisedSection()
    {
        // A typo must not be silently ignored, or it surfaces later as "build.project not set".
        WriteRittenJson(_root, """{ "biuld": { "project": "src/Thing/Thing.csproj" } }""");
        var project = await RittenProject.Resolve(_root);

        var settings = project.Value.ShouldNotBeNull().GetSettings<DotNetToolSettings>();

        settings.IsError.ShouldBeTrue();
        var error = settings.Errors.ShouldHaveSingleItem();
        // The message says what to fix without needing --verbose; the exception is kept for it.
        error.Message.ShouldContain(RittenProject.FileName);
        error.Message.ShouldContain("biuld");
        error.Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task GetSettings_RejectsAnUnrecognisedKeyWithinASection()
    {
        WriteRittenJson(_root, """{ "build": { "projct": "src/Thing/Thing.csproj" } }""");
        var project = await RittenProject.Resolve(_root);

        var settings = project.Value.ShouldNotBeNull().GetSettings<DotNetToolSettings>();

        settings.IsError.ShouldBeTrue();
        var error = settings.Errors.ShouldHaveSingleItem();
        // The message says what to fix without needing --verbose; the exception is kept for it.
        error.Message.ShouldContain(RittenProject.FileName);
        error.Message.ShouldContain("projct");
        error.Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task GetSettings_RejectsAKeyBelongingToAnotherKindOfProject()
    {
        // Because the settings type belongs to the pipeline, a key that would be valid for an
        // npm project is an error in a .NET one.
        WriteRittenJson(_root, """{ "build": { "directory": "packages/thing" } }""");
        var project = await RittenProject.Resolve(_root);

        var settings = project.Value.ShouldNotBeNull().GetSettings<DotNetToolSettings>();

        settings.IsError.ShouldBeTrue();
        var error = settings.Errors.ShouldHaveSingleItem();
        // The message says what to fix without needing --verbose; the exception is kept for it.
        error.Message.ShouldContain(RittenProject.FileName);
        error.Message.ShouldContain("directory");
        error.Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task GetSettings_AllowsCommentsAndTrailingCommas()
    {
        // It's a hand-edited file.
        WriteRittenJson(_root, """
            {
                // The package this project ships.
                "build": { "project": "src/Thing/Thing.csproj", },
            }
            """);

        var settings = await GetSettings(_root);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
    }

    private static async Task<DotNetToolSettings> GetSettings(string directory)
    {
        var project = await RittenProject.Resolve(directory);
        var settings = project.Value.ShouldNotBeNull().GetSettings<DotNetToolSettings>();
        settings.IsError.ShouldBeFalse();
        return settings.Value;
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.FileName), content);
    }
}
