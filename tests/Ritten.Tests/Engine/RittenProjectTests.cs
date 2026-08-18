using System.Text.Json;
using Ritten.Engine;

namespace Ritten.Tests.Engine;

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

        var project = await RittenProject.Resolve(_root, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        project.Value.ShouldNotBeNull().Directory.ShouldBe(_root);
    }

    [Fact]
    public async Task Resolve_WalksUpFromASubdirectory()
    {
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "src", "Thing", "bin")).FullName;

        var project = await RittenProject.Resolve(nested, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        project.Value.ShouldNotBeNull().Directory.ShouldBe(_root);
    }

    [Fact]
    public async Task Resolve_PrefersTheNearestFile()
    {
        // A nested project shadows the outer one.
        WriteRittenJson(_root);
        var nested = Directory.CreateDirectory(Path.Combine(_root, "nested")).FullName;
        WriteRittenJson(nested);

        var project = await RittenProject.Resolve(Path.Combine(nested, "src"), RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        project.Value.ShouldNotBeNull().Directory.ShouldBe(nested);
    }

    [Fact]
    public async Task Resolve_ReportsNoProjectUpToTheFilesystemRoot()
    {
        Directory.CreateDirectory(_root);

        var project = await RittenProject.Resolve(_root, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        project.IsError.ShouldBeTrue();
        project.Errors.ShouldHaveSingleItem().Message.ShouldContain("No ritten.json found");
    }

    [Fact]
    public async Task Resolve_ReportsMalformedJsonWithoutThrowing()
    {
        WriteRittenJson(_root, "{ not json");

        var project = await RittenProject.Resolve(_root, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        project.IsError.ShouldBeTrue();
        var error = project.Errors.ShouldHaveSingleItem();
        error.Message.ShouldContain("Could not read");
        error.Cause.ShouldBeOfType<JsonException>();
    }

    [Fact]
    public async Task GetWorkflowName_ReadsTheDeclaration()
    {
        WriteRittenJson(_root, """{ "workflow": "dotnet-tool", "build": { "project": "src/Thing/Thing.csproj" } }""");

        var project = await RittenProject.Resolve(_root, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);

        project.Value.ShouldNotBeNull().GetWorkflowName().Value.ShouldBe("dotnet-tool");
    }

    [Fact]
    public async Task GetWorkflowName_ReportsAMissingDeclaration()
    {
        WriteRittenJson(_root);

        var project = await RittenProject.Resolve(_root, RittenProject.DefaultFileName, TestContext.Current.CancellationToken);
        var name = project.Value.ShouldNotBeNull().GetWorkflowName();

        name.IsError.ShouldBeTrue();
        var error = name.Errors.ShouldHaveSingleItem();
        error.Message.ShouldContain(RittenProject.DefaultFileName);
        error.Message.ShouldContain("workflow");
    }

    [Fact]
    public async Task Resolve_FindsAProjectFileTheHostRenamed()
    {
        // An embedding host names the file itself, so building on Ritten needn't be announced.
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "build.json"), "{}");

        var project = await RittenProject.Resolve(_root, "build.json", TestContext.Current.CancellationToken);

        project.Value.ShouldNotBeNull().FilePath.ShouldBe(Path.Combine(_root, "build.json"));
    }

    private static void WriteRittenJson(string directory, string content = "{}")
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, RittenProject.DefaultFileName), content);
    }
}
