using Microsoft.Extensions.DependencyInjection;
using Ritten.Engine;
using Ritten.Tests.Engine.Helpers;

namespace Ritten.Tests.Engine;

/// <summary>
/// The project file is read as a document rather than as settings, so that a job which fills in
/// what's missing leaves everything else — including keys this version has never heard of —
/// exactly as it found them.
/// </summary>
public class ProjectFileTests
{
    private static readonly IProjectFiles Files = WorkflowRunBuilderHelpers.Create()
        .Services.BuildServiceProvider()
        .GetRequiredService<IProjectFiles>();

    [Fact]
    public void DeclaresTheWorkflowWhereAReaderLooksForIt()
    {
        var document = Parse("""{ "build": { "project": "src/Thing/Thing.csproj" } }""");

        document.Workflow = "dotnet-tool";

        Files.Render(document).ShouldStartWith("{\n  \"workflow\": \"dotnet-tool\",".ReplaceLineEndings("\n"));
    }

    [Fact]
    public void RefusesAProjectFileThatIsNotJson()
    {
        // Reading is a client call, so a file somebody broke fails the step that reads it rather
        // than throwing out of it.
        var read = Files.Parse("{ not json");

        read.IsError.ShouldBeTrue();
    }

    [Fact]
    public void KeepsKeysItHasNeverHeardOf()
    {
        // A project file written by a newer tool still round-trips through an older one.
        var document = Parse("""{ "workflow": "dotnet-tool", "somethingNewer": { "keep": "me" } }""");

        document.Set("build.project", "src/Thing/Thing.csproj");

        Files.Render(document).ShouldContain("\"keep\": \"me\"");
    }

    [Fact]
    public void WritesNestedKeysByPath()
    {
        var document = ProjectFile.Empty;
        document.Workflow = "dotnet-tool";
        document.Set("build.projects", ["src/A/A.csproj", "src/B/B.csproj"]);

        Files.Render(document).ShouldBe(
            """
            {
              "workflow": "dotnet-tool",
              "build": {
                "projects": [
                  "src/A/A.csproj",
                  "src/B/B.csproj"
                ]
              }
            }

            """.ReplaceLineEndings("\n"));
    }

    private static ProjectFile Parse(string json) => Files.Parse(json).Value.ShouldNotBeNull();

    [Theory]
    [InlineData("workflow", true)]
    [InlineData("build.project", true)]
    [InlineData("build.projects", false)]
    [InlineData("release.tagPrefix", false)]
    public void AnswersWhetherItAlreadySaysSomething(string key, bool expected)
    {
        var document = Parse("""{ "workflow": "dotnet-tool", "build": { "project": "src/Thing/Thing.csproj" } }""");

        document.Has(key).ShouldBe(expected);
    }

    [Fact]
    public void ReadsAnEmptyDocumentAsOneNobodyHasWritten()
    {
        // A repository being set up has no project file, which is a document with nothing in it
        // rather than an error.
        var document = Parse("");

        document.Workflow.ShouldBeNull();
        document.Has("build").ShouldBeFalse();
    }
}
