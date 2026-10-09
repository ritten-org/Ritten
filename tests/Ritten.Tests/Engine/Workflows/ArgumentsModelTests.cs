using System.Text.Json;
using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Releases;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Engine.Workflows;

public class ArgumentsModelTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-arguments-{Guid.NewGuid():N}");

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

        var settings = await Read(_root, TestContext.Current.CancellationToken);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Build.Configuration.ShouldBe("Debug");
        settings.Repository.ShouldBe(new PackageRepository("https://example.com/thing"));
        settings.Changelog.File.ShouldBe("HISTORY.md");
        settings.Release.TagPrefix.ShouldBe("release-");
        settings.Release.Feed.ShouldBe("https://example.com/index.json");
    }

    [Fact]
    public async Task Read_AppliesDefaultsForOmittedSections()
    {
        WriteRittenJson(_root);

        var settings = await Read(_root, TestContext.Current.CancellationToken);

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

        var settings = await Read(_root, TestContext.Current.CancellationToken);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
        settings.Build.Configuration.ShouldBe("Release");
    }

    [Fact]
    public async Task Read_ReadsEnumValuesAsCamelCaseStrings()
    {
        WriteRittenJson(_root, """{ "release": { "lines": "minor" } }""");

        var settings = await Read(_root, TestContext.Current.CancellationToken);

        settings.Release.Lines.ShouldBe(ReleaseLine.Minor);
    }

    [Fact]
    public async Task Read_ReadsTheCoverageSection()
    {
        WriteRittenJson(_root, """{ "coverage": { "line": 80.5 } }""");

        var settings = await Read(_root, TestContext.Current.CancellationToken);

        settings.Coverage.Line.ShouldBe(80.5m);
        settings.Coverage.Branch.ShouldBeNull();
    }

    [Fact]
    public async Task Read_AppliesCoverageDefaultsWithoutItsSection()
    {
        // Coverage is always on; the section only sets minimums.
        WriteRittenJson(_root);

        var settings = await Read(_root, TestContext.Current.CancellationToken);

        settings.Coverage.Line.ShouldBeNull();
        settings.Coverage.Branch.ShouldBeNull();
    }

    [Fact]
    public async Task Read_RejectsAnUnrecognisedEnumValue()
    {
        WriteRittenJson(_root, """{ "release": { "lines": "patch" } }""");
        var project = await RittenProject.Resolve(_root, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        var settings = Model.Read(project.Value.ShouldNotBeNull(), None);

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

        var settings = await Read(_root, TestContext.Current.CancellationToken);

        settings.Build.Project.ShouldBe("src/Thing/Thing.csproj");
    }

    [Fact]
    public void Read_SetsTheValuesGivenOnTheCommandLine()
    {
        var model = ArgumentsModel.For(typeof(PrepareDotNetToolArguments));
        var version = model.Options.ShouldHaveSingleItem();

        var read = model.Read(Project("{}"), new Dictionary<JobOption, object?> { [version] = version.Read("1.2.0").Value });

        read.Value.ShouldBeOfType<PrepareDotNetToolArguments>().Version.ShouldBe(new RequestedVersion(NuGetVersion.Parse("1.2.0")));
    }

    [Fact]
    public void Read_LeavesTheProjectFileNoSayInACommandLineValue()
    {
        // The command line owns what it declares, so a project file can't quietly answer for it.
        var read = ArgumentsModel.For(typeof(PrepareDotNetToolArguments)).Read(Project("""{ "version": "9.9.9" }"""), None);

        read.Value.ShouldBeOfType<PrepareDotNetToolArguments>().Version.ShouldBe(RequestedVersion.None);
    }

    [Fact]
    public void Read_ReadsAGivenFlag()
    {
        var model = ArgumentsModel.For(typeof(InstallDotNetToolArguments));
        var force = model.Options.ShouldHaveSingleItem();

        var read = model.Read(Project("{}"), new Dictionary<JobOption, object?> { [force] = true });

        force.IsFlag.ShouldBeTrue();
        read.Value.ShouldBeOfType<InstallDotNetToolArguments>().Reinstall.ShouldBe(new ForceReinstall(true));
    }

    [Fact]
    public void Seed_PutsEachValueIntoTheStateUnderItsType()
    {
        var arguments = new DotNetToolArguments { Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj" } };
        Dictionary<Type, object> state = [];

        Model.Seed(arguments, state);

        state[typeof(DotNetBuildSettings)].ShouldBeSameAs(arguments.Build);
        state[typeof(ReleaseSettings)].ShouldBeSameAs(arguments.Release);
        state.ShouldNotContainKey(typeof(PackageRepository));
    }

    [Fact]
    public void Validate_RefusesTwoValuesOfOneType() =>
        ArgumentsModel.For(typeof(Ambiguous)).Validate().ShouldHaveSingleItem().Message.ShouldContain("several properties of type ChangelogSettings");

    [Fact]
    public void Validate_RefusesAScalar() =>
        ArgumentsModel.For(typeof(Scalar)).Validate().ShouldHaveSingleItem().Message.ShouldContain("Scalar.Name is a String");

    [Fact]
    public void Validate_AcceptsTheWorkflowsOwnArguments()
    {
        ArgumentsModel.For(typeof(PrepareDotNetToolArguments)).Validate().ShouldBeEmpty();
        ArgumentsModel.For(typeof(InstallDotNetToolArguments)).Validate().ShouldBeEmpty();
    }

    private sealed record Ambiguous(ChangelogSettings First, ChangelogSettings Second);

    private sealed record Scalar(string Name);

    private static ArgumentsModel Model => ArgumentsModel.For(typeof(DotNetToolArguments));

    private static IReadOnlyDictionary<JobOption, object?> None { get; } = new Dictionary<JobOption, object?>();

    private static RittenProject Project(string settings) => new()
    {
        Directory = Path.GetTempPath(),
        Settings = JsonSerializer.Deserialize<JsonElement>(settings)
    };

    private static async Task<DotNetToolArguments> Read(string directory, CancellationToken ct)
    {
        var project = await RittenProject.Resolve(directory, RittenProject.DefaultFileName, ct);
        var settings = Model.Read(project.Value.ShouldNotBeNull(), None);
        settings.IsError.ShouldBeFalse();
        return settings.Value.ShouldBeOfType<DotNetToolArguments>();
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.DefaultFileName), content);
    }
}
