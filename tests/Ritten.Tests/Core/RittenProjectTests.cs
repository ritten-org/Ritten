using System.Text.Json;
using Ritten.Core;
using Ritten.Pipelines.DotNet;

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

        var project = await Resolve(_root);

        project.ShouldNotBeNull().Directory.ShouldBe(_root);
    }

    [Fact]
    public async Task Resolve_WalksUpFromASubdirectory()
    {
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "src", "Thing", "bin")).FullName;

        var project = await Resolve(nested);

        project.ShouldNotBeNull().Directory.ShouldBe(_root);
    }

    [Fact]
    public async Task Resolve_PrefersTheNearestFile()
    {
        // A nested project shadows the outer one.
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "nested")).FullName;
        WriteRittenJson(nested);

        var project = await Resolve(Path.Combine(nested, "src"));

        project.ShouldNotBeNull().Directory.ShouldBe(nested);
    }

    [Fact]
    public async Task Resolve_ReturnsNullWhenNoFileExistsUpToTheFilesystemRoot()
    {
        Directory.CreateDirectory(_root);

        var project = await Resolve(_root);

        project.ShouldBeNull();
    }

    [Fact]
    public async Task Resolve_ThrowsOnMalformedJson()
    {
        WriteRittenJson(_root, "{ not json");

        // PipelineHost turns this into a configuration error rather than a crash.
        await Should.ThrowAsync<JsonException>(async () => await Resolve(_root));
    }

    [Fact]
    public async Task Bind_ReadsCamelCaseKeys()
    {
        WriteRittenJson(_root, """{ "project": "src/Thing/Thing.csproj", "changelog": "HISTORY.md" }""");

        var settings = await Bind(_root);

        settings.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Changelog.ShouldBe("HISTORY.md");
    }

    [Fact]
    public async Task Bind_AppliesDefaultsForOmittedKeys()
    {
        WriteRittenJson(_root);

        var settings = await Bind(_root);

        settings.Project.ShouldBeNull();
        settings.Configuration.ShouldBe("Release");
        settings.Changelog.ShouldBe("CHANGELOG.md");
        settings.TagPrefix.ShouldBe("v");
        settings.Feed.ShouldBe("https://api.nuget.org/v3/index.json");
    }

    [Fact]
    public async Task Bind_RejectsAKeyThePipelineDoesNotRecognise()
    {
        // A typo must not be silently ignored, or it surfaces later as "project must be set".
        // Because the settings type belongs to the pipeline, this also catches a key that would
        // have been valid for a different kind of project.
        WriteRittenJson(_root, """{ "projct": "src/Thing/Thing.csproj" }""");
        var project = await Resolve(_root);

        var act = () => project!.GetSettings(typeof(DotNetPackageSettings));

        act.ShouldThrow<JsonException>().Message.ShouldContain("projct");
    }

    [Fact]
    public async Task Bind_AllowsCommentsAndTrailingCommas()
    {
        // It's a hand-edited file.
        WriteRittenJson(_root, """
            {
                // The package this project ships.
                "project": "src/Thing/Thing.csproj",
            }
            """);

        var settings = await Bind(_root);

        settings.Project.ShouldBe("src/Thing/Thing.csproj");
    }

    private static Task<RittenProject?> Resolve(string directory) => RittenProject.Resolve(directory);

    private static async Task<DotNetPackageSettings> Bind(string directory)
    {
        var project = await Resolve(directory);
        return (DotNetPackageSettings)project.ShouldNotBeNull().GetSettings(typeof(DotNetPackageSettings));
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.FileName), content);
    }
}
