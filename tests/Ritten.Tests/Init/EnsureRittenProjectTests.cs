using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Init.Steps;
using Ritten.Reporting;
using Ritten.Tests.Engine.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Init;

/// <summary>
/// Exercises the real document client, so what these tests assert is the file that lands on disk.
/// </summary>
public class EnsureRittenProjectTests
{
    private static readonly IProjectFiles Files = WorkflowRunBuilderHelpers.Create()
        .Services.BuildServiceProvider()
        .GetRequiredService<IProjectFiles>();

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IWorkflowPrompt _prompt = Substitute.For<IWorkflowPrompt>();
    private MemoryStream _written = new();

    public EnsureRittenProjectTests()
    {
        _prompt.IsInteractive.Returns(true);
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task WritesTheWorkflowAndTheProjectItBuilds()
    {
        var file = SetProjectFile(exists: false);

        var result = await Step().Run(Found("src/Thing/Thing.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        file.Received().OpenWrite();
        Written().ShouldContain("\"workflow\": \"dotnet-tool\"");
        Written().ShouldContain("\"project\": \"src/Thing/Thing.csproj\"");
    }

    [Fact]
    public async Task WritesEveryProjectWhenTheRepositoryShipsSeveral()
    {
        SetProjectFile(exists: false);

        await Step().Run(Found("src/A/A.csproj", "src/B/B.csproj"), TestContext.Current.CancellationToken);

        // One package is spelled singular and several plural: the same setting, said the way the
        // repository would say it.
        Written().ShouldContain("\"projects\"");
        Written().ShouldContain("src/B/B.csproj");
    }

    [Fact]
    public async Task LeavesWhatTheProjectFileAlreadySays()
    {
        var file = SetProjectFile(exists: true, content: """{ "workflow": "dotnet-tool", "build": { "projects": ["src/Only/Only.csproj"] } }""");

        var result = await Step().Run(Found("src/Thing/Thing.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        file.DidNotReceive().OpenWrite();
    }

    [Fact]
    public async Task KeepsKeysItHasNeverHeardOf()
    {
        SetProjectFile(exists: true, content: """{ "somethingNewer": { "keep": "me" } }""");

        await Step().Run(Found("src/Thing/Thing.csproj"), TestContext.Current.CancellationToken);

        Written().ShouldContain("\"keep\": \"me\"");
        Written().ShouldContain("\"workflow\": \"dotnet-tool\"");
    }

    [Fact]
    public async Task AsksBeforeWritingDownAWorkflowNobodyDeclared()
    {
        var file = SetProjectFile(exists: false);
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Step().Run(Found("src/Thing/Thing.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        file.DidNotReceive().OpenWrite();
    }

    [Fact]
    public async Task RefusesToGuessWithNobodyThereToConfirm()
    {
        // Hanging on a build agent waiting for a person is worse than refusing to start.
        SetProjectFile(exists: false);
        _prompt.IsInteractive.Returns(false);

        var result = await Step().Run(Found("src/Thing/Thing.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--workflow");
    }

    [Fact]
    public async Task AsksNothingWhenTheProjectDeclaredTheWorkflow()
    {
        // Only a guess is worth confirming; topping up a repository that already said what it
        // runs is not.
        SetProjectFile(exists: true, content: """{ "workflow": "dotnet-tool" }""");

        await Step(recognised: null).Run(Found("src/Thing/Thing.csproj"), TestContext.Current.CancellationToken);

        await _prompt.DidNotReceive().Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static DiscoveredProjects Found(params string[] projects) => new(projects, []);

    private EnsureRittenProject Step(string? recognised = "src/Thing/Thing.csproj packs as a tool") => new(
        Substitute.For<IWorkflowLog>(),
        Files,
        _fileSystem,
        Project,
        new SelectedWorkflow(new TestWorkflow("dotnet-tool", label: "dotnet tool"), Project, recognised),
        new WorkflowJob("dotnet tool", "init"),
        _prompt
    );

    /// <summary>A repository that hasn't written a project file yet.</summary>
    private static RittenProject Project { get; } = RittenProject.Synthetic(Path.GetTempPath(), RittenProject.DefaultFileName);

    private string Written() => Encoding.UTF8.GetString(_written.ToArray());

    private IFile SetProjectFile(bool exists, string content = "")
    {
        _written = new MemoryStream();
        var file = Substitute.For<IFile>();
        file.Name.Returns(RittenProject.DefaultFileName);
        file.Exists.Returns(exists);
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        file.OpenWrite().Returns(_ => _written);
        _fileSystem.ProjectRoot.GetFile(RittenProject.DefaultFileName).Returns(file);
        return file;
    }
}
