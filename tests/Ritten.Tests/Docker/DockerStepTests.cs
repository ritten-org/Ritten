using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Docker;
using Ritten.Docker.Steps;
using Ritten.Engine.FileSystem;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Docker;

public class DockerStepTests : IDisposable
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-docker-{Guid.NewGuid():N}");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public DockerStepTests()
    {
        Directory.CreateDirectory(_root);
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(_root));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_root, true);
    }

    [Fact]
    public async Task ComposeCheck_PassesWhenComposeCanReadTheFile()
    {
        var result = await new ComposeCheck(Docker(), _fileSystem, _log).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task ComposeCheck_FailsWithWhatComposeSaidRatherThanAGenericMessage()
    {
        // The compose error is the whole value of the step: "the compose file is invalid" would
        // send the reader back to a terminal to find out what this already knows.
        _commands.Respond(c => c.Arguments.Contains("config"), new CommandResult(1, "", "services.web.ports: invalid"));

        var result = await new ComposeCheck(Docker(), _fileSystem, _log).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("services.web.ports: invalid");
    }

    [Fact]
    public async Task BuildImages_DoesNothingWhenTheComponentDeclaresNone()
    {
        var result = await BuildImages([]).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _commands.Executed.ShouldBeEmpty();
    }

    [Fact]
    public async Task BuildImages_BuildsEachDeclaredImageFromItsOwnContext()
    {
        Dockerfile("watcher");
        Dockerfile("sidecar");

        var result = await BuildImages([new DockerImage("lab/watcher", "watcher"), new DockerImage("lab/sidecar", "sidecar")])
            .Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        _commands.Executed.Select(c => c.Arguments[2]).ShouldBe(["lab/watcher", "lab/sidecar"]);
    }

    [Fact]
    public async Task BuildImages_SaysWhichContextHasNoDockerfileRatherThanLettingDockerSayIt()
    {
        var result = await BuildImages([new DockerImage("lab/watcher", "watcher")]).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain(Path.Combine(_root, "watcher"));
        _commands.Executed.ShouldBeEmpty();
    }

    private DockerClient Docker() => new(_commands);

    private BuildImages BuildImages(IReadOnlyList<DockerImage> images) =>
        new(Options.Create(new DockerOptions { Images = images }), Docker(), _fileSystem, _log);

    private void Dockerfile(string context)
    {
        Directory.CreateDirectory(Path.Combine(_root, context));
        File.WriteAllText(Path.Combine(_root, context, "Dockerfile"), "FROM scratch\n");
    }
}
