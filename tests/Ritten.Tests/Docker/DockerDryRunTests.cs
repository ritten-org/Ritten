using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Docker;

// What a dry run's command runner lets the docker client do: probe, but change no image, container or stack.
public class DockerDryRunTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly DockerClient _docker;

    public DockerDryRunTests() => _docker = new DockerClient(new DryRunCommandRunner(Substitute.For<IWorkflowLog>(), _commands));

    private static IDirectory Stack => new PhysicalDirectory("/srv/stack");

    [Fact]
    public async Task Exec_RunsARead()
    {
        _commands.Respond(_ => true, new CommandResult(0, "version 3", ""));

        var read = new ContainerExec("garage", ["/garage", "layout", "show"]) { IsReadOnly = true };

        (await _docker.Exec(read, TestContext.Current.CancellationToken)).StandardOutput.ShouldBe("version 3");
    }

    [Fact]
    public async Task Exec_SkipsAnythingElse()
    {
        var result = await _docker.Exec(new ContainerExec("caddy", ["caddy", "reload"]), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _commands.Executed.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheStacksAreReadButNotChanged()
    {
        _commands.Respond(command => command.Arguments[0] == "inspect", new CommandResult(0, "caddy:2 true", ""));
        await _docker.ComposeImages(Stack, ct: TestContext.Current.CancellationToken);
        await _docker.ComposeValidate(Stack, ct: TestContext.Current.CancellationToken);
        await _docker.Inspect("caddy", TestContext.Current.CancellationToken);
        await _docker.ComposeUp(Stack, ct: TestContext.Current.CancellationToken);
        await _docker.ComposeDown(Stack, TestContext.Current.CancellationToken);
        await _docker.ComposeStop(Stack, TestContext.Current.CancellationToken);
        await _docker.ComposeStart(Stack, TestContext.Current.CancellationToken);
        await _docker.Push("code.twolfe.dev/lab/ci:1", TestContext.Current.CancellationToken);
        await _docker.Run(new ContainerRun("alpine", ["true"]), TestContext.Current.CancellationToken);

        _commands.Executed.Select(command => command.Arguments[0] == "compose" ? command.Arguments[3] : command.Arguments[0])
            .ShouldBe(["images", "config", "inspect"]);
    }

    [Fact]
    public async Task RemoveImage_RemovesNothing()
    {
        var objection = await _docker.RemoveImage("1a08d8dfc2b5", TestContext.Current.CancellationToken);

        objection.ShouldBeNull();
        _commands.Executed.ShouldBeEmpty();
    }
}
