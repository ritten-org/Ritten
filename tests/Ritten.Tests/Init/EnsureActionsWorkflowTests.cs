using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.Init;
using Ritten.Init.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Init;

/// <summary>
/// The workflow file is the repository's: Ritten finds the jobs it wrote by what they run, so a
/// file that has been renamed is updated rather than duplicated, and two projects in one
/// repository never claim the same file.
/// </summary>
public class EnsureActionsWorkflowTests
{
    private readonly IActionsWorkflows _actions = Substitute.For<IActionsWorkflows>();
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _repository = Directory("/repo");
    private readonly IDirectory _root = Directory("/repo");
    private readonly IDirectory _nested = Directory("/repo/services/api");
    private readonly IFile _globalJson = File("global.json", exists: true);
    private readonly List<IFile> _existing = [];
    private readonly Dictionary<string, IFile> _files = [];
    private readonly IFile _fresh = File("new.yml", exists: false);
    private string _written = "";
    private string? _created;

    public EnsureActionsWorkflowTests()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(_repository);
        _fileSystem.ProjectRoot.Returns(_root);
        _repository.GetFile(Arg.Any<string>()).Returns(_globalJson);

        _actions.Files(Arg.Any<IDirectory>()).Returns(_ => _existing);
        _actions.Parse(Arg.Any<string>()).Returns(call => new Result<ActionsWorkflow>(ActionsWorkflow.Parse(call.Arg<string>())));
        _actions.File(Arg.Any<IDirectory>(), Arg.Any<string>()).Returns(call =>
        {
            _created = call.ArgAt<string>(1);
            return _files.TryGetValue(_created, out var file) ? file : _fresh;
        });
        _actions.Render(Arg.Any<ActionsWorkflow>()).Returns(call => call.Arg<ActionsWorkflow>().Text);
        _actions.Write(Arg.Any<IFile>(), Arg.Any<ActionsWorkflow>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _written = call.ArgAt<ActionsWorkflow>(1).Text;
                return Task.CompletedTask;
            });
    }

    [Fact]
    public async Task NamesTheWorkflowAfterTheProjectItBuilds()
    {
        // Not after the tool that wrote it: that would be the same name in every repository, and
        // the same name twice in a repository of several projects.
        var result = await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _created.ShouldBe("my-tool");
        _written.ShouldContain("name: My.Tool");
        _written.ShouldContain("run: dotnet ritten check");
        _written.ShouldContain("run: dotnet ritten deploy");

        // Nothing to say: the project is the repository, so every step runs where it lands.
        _written.ShouldNotContain("working-directory:");
    }

    [Fact]
    public async Task PrefersTheProjectTheRepositoryDeclares()
    {
        // The first declared project is the face of whatever the repository ships; what's on disk
        // only answers for a repository that hasn't declared anything yet.
        await Step(declared: "src/My.Package/My.Package.csproj")
            .Run(Found("src/Another/Another.csproj"), TestContext.Current.CancellationToken);

        _written.ShouldContain("name: My.Package");
    }

    [Fact]
    public async Task GivesANestedProjectItsOwnNameAndDirectory()
    {
        _fileSystem.ProjectRoot.Returns(_nested);

        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        _created.ShouldBe("my-tool");
        _written.ShouldContain("name: My.Tool");
        _written.ShouldContain("working-directory: services/api");
    }

    [Fact]
    public async Task NamesTheFileForTheDirectoryWhenAProjectOfTheSameNameGotThereFirst()
    {
        // Two projects can share a name in one repository even though their paths can't, and
        // ensuring one project's jobs must never overwrite another's.
        _fileSystem.ProjectRoot.Returns(_nested);
        SetWorkflow("my-tool.yml",
            """
            name: My.Tool

            jobs:
              check:
                steps:
                  - run: dotnet ritten check
                    working-directory: legacy/api
            """);

        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        _created.ShouldBe("my-tool-services-api");
    }

    [Fact]
    public async Task FindsItsOwnWorkflowHoweverItWasRenamed()
    {
        var ci = SetWorkflow("ci.yml",
            """
            # Ours, hand tended.
            name: CI

            on:
              push:
                branches: [ main ]

            jobs:
              check:
                runs-on: ubuntu-latest
                steps:
                  - run: dotnet ritten check
            """);

        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        // Found by what it runs, so the rename is followed rather than duplicated.
        _created.ShouldBeNull();
        await _actions.Received().Write(ci, Arg.Any<ActionsWorkflow>(), Arg.Any<CancellationToken>());
        _written.ShouldContain("# Ours, hand tended.");
        _written.ShouldContain("run: dotnet ritten deploy");
    }

    [Fact]
    public async Task LeavesAnotherProjectsWorkflowAlone()
    {
        // Same jobs, same commands, different project: this file belongs to services/web.
        var web = SetWorkflow("my-lib.yml",
            """
            name: My.Lib

            on:
              push:
                branches: [ main ]

            jobs:
              check:
                steps:
                  - run: dotnet ritten check
                    working-directory: services/web
            """);
        _fileSystem.ProjectRoot.Returns(_nested);

        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        _created.ShouldBe("my-tool");
        await _actions.DidNotReceive().Write(web, Arg.Any<ActionsWorkflow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WritesNothingWhenTheWorkflowAlreadyRunsTheJobs()
    {
        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);
        var first = _written;
        SetWorkflow("my-tool.yml", first);
        _actions.ClearReceivedCalls();

        var result = await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _actions.DidNotReceive().Write(Arg.Any<IFile>(), Arg.Any<ActionsWorkflow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoresTheJobItOwnsWhenItHasBeenEditedAway()
    {
        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);
        SetWorkflow("my-tool.yml", _written.Replace("      pull-requests: write\n", ""));

        await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        _written.ShouldContain("pull-requests: write");
    }

    [Fact]
    public async Task SaysSoWhenThereIsNoRepositoryToWriteInto()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns((IDirectory?)null);

        var result = await Step().Run(Found("src/My.Tool/My.Tool.csproj"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _actions.DidNotReceive().Write(Arg.Any<IFile>(), Arg.Any<ActionsWorkflow>(), Arg.Any<CancellationToken>());
    }

    private static DiscoveredProjects Found(params string[] projects) => new(projects, []);

    private EnsureActionsWorkflow Step(string declared = "") => new(
        Substitute.For<IWorkflowLog>(),
        _actions,
        _git,
        _fileSystem,
        Options.Create(new DotNetOptions { ProjectFile = declared }),
        new SelectedWorkflow(
            new TestWorkflow("dotnet-tool", [
                new TestJob(name: "build"),
                new TestJob(name: "check", kind: JobKind.Check),
                new TestJob(name: "deploy", kind: JobKind.Deploy)
            ], label: "dotnet tool"),
            RittenProject.Synthetic(Path.GetTempPath(), RittenProject.DefaultFileName)),
        new ToolPin("ritten", "ritten", NuGetVersion.Parse("1.2.3"))
    );

    private IFile SetWorkflow(string name, string content)
    {
        var file = File(name, exists: true);
        var parsed = new Result<ActionsWorkflow>(ActionsWorkflow.Parse(content));
        _actions.Read(file, Arg.Any<CancellationToken>()).Returns(parsed);
        _existing.Clear();
        _existing.Add(file);
        _files[Path.GetFileNameWithoutExtension(name)] = file;
        return file;
    }

    private static IDirectory Directory(string path)
    {
        var directory = Substitute.For<IDirectory>();
        directory.AbsolutePath.Returns(path);
        return directory;
    }

    private static IFile File(string name, bool exists)
    {
        var file = Substitute.For<IFile>();
        file.Name.Returns(name);
        file.Exists.Returns(exists);
        return file;
    }
}
