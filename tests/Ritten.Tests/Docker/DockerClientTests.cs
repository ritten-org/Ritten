using Ritten.Commands;
using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Ritten.Tests.Support;

namespace Ritten.Tests.Docker;

public class DockerClientTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly DockerClient _docker;

    public DockerClientTests() => _docker = new DockerClient(_commands);

    [Fact]
    public async Task Build_TagsTheContextDirectory()
    {
        await _docker.Build(new PhysicalDirectory("/src/tool"), "org/tool:1.0", ct: TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Path.ShouldBe("docker");
        command.Arguments.ShouldBe(["build", "--tag", "org/tool:1.0", Path.GetFullPath("/src/tool")]);
    }

    [Fact]
    public async Task Build_NamesThePlatformWhenGivenOne()
    {
        await _docker.Build(new PhysicalDirectory("/src/tool"), "org/tool:1.0", "linux/amd64", TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldContain("--platform");
        _commands.Executed.Single().Arguments.ShouldContain("linux/amd64");
    }

    [Fact]
    public async Task ComposeValidate_SaysNothingWhenTheFileIsGood()
    {
        var error = await _docker.ComposeValidate(new PhysicalDirectory("/src/stack"), ct: TestContext.Current.CancellationToken);

        error.ShouldBeNull();
        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["compose", "--project-directory", Path.GetFullPath("/src/stack"), "config", "--quiet"]);
        // What compose objected to is the answer here, so a bad file must come back rather than throw.
        command.ThrowsOnError.ShouldBeFalse();
    }

    [Fact]
    public async Task ComposeValidate_HandsBackWhatComposeObjectedTo()
    {
        _commands.Respond(c => c.Arguments.Contains("config"), new CommandResult(1, "", "services.web.ports: invalid\n"));

        var error = await _docker.ComposeValidate(new PhysicalDirectory("/src/stack"), ct: TestContext.Current.CancellationToken);

        error.ShouldBe("services.web.ports: invalid");
    }

    [Fact]
    public async Task ComposeValidate_FallsBackToStandardOutputWhenComposeSaysNothingOnError()
    {
        _commands.Respond(c => c.Arguments.Contains("config"), new CommandResult(1, "no configuration file provided\n", ""));

        var error = await _docker.ComposeValidate(new PhysicalDirectory("/src/stack"), ct: TestContext.Current.CancellationToken);

        error.ShouldBe("no configuration file provided");
    }

    [Fact]
    public async Task Login_HandsThePasswordOverStandardInput()
    {
        await _docker.Login("registry.example.com", "AWS", "s3cret", TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["login", "--username", "AWS", "--password-stdin", "registry.example.com"]);
        command.StandardInput.ShouldBe("s3cret");
    }

    [Fact]
    public async Task Run_KeepsEnvironmentValuesOffTheCommandLine()
    {
        var run = new ContainerRun("org/tool:1.0", ["upload", "/takeout"])
        {
            Mounts = [new BindMount(new PhysicalDirectory("/data/takeout"), "/takeout", ReadOnly: true)],
            Environment = new Dictionary<string, string> { ["API_KEY"] = "s3cret" },
            Network = "lab"
        };

        await _docker.Run(run, TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe([
            "run", "--rm", "--network", "lab",
            "--volume", $"{Path.GetFullPath("/data/takeout")}:/takeout:ro",
            "--env", "API_KEY",
            "org/tool:1.0", "upload", "/takeout"
        ]);
        command.EnvironmentVariables["API_KEY"].ShouldBe("s3cret");
    }

    [Fact]
    public async Task ComposeUp_ConvergesFromTheProjectWithTheEnvironmentGiven()
    {
        await _docker.ComposeUp(new PhysicalDirectory("/srv/stack"), new Dictionary<string, string> { ["DB_PASSWORD"] = "pg" }, TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["compose", "--project-directory", Path.GetFullPath("/srv/stack"), "up", "-d", "--remove-orphans"]);
        command.EnvironmentVariables["DB_PASSWORD"].ShouldBe("pg");
    }

    [Fact]
    public async Task ComposeDown_LeavesVolumesAlone()
    {
        await _docker.ComposeDown(new PhysicalDirectory("/srv/stack"), TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["compose", "--project-directory", Path.GetFullPath("/srv/stack"), "down"]);
    }

    [Fact]
    public async Task Tag_And_Push_SpellTheirVerbs()
    {
        await _docker.Tag("org/tool:1.0", "org/tool:latest", TestContext.Current.CancellationToken);
        await _docker.Push("org/tool:latest", TestContext.Current.CancellationToken);

        _commands.Executed[0].Arguments.ShouldBe(["tag", "org/tool:1.0", "org/tool:latest"]);
        _commands.Executed[1].Arguments.ShouldBe(["push", "org/tool:latest"]);
    }

    [Fact]
    public async Task ComposeStop_And_ComposeStart_LeaveContainersInPlace()
    {
        await _docker.ComposeStop(new PhysicalDirectory("/srv/stack"), TestContext.Current.CancellationToken);
        await _docker.ComposeStart(new PhysicalDirectory("/srv/stack"), TestContext.Current.CancellationToken);

        _commands.Executed[0].Arguments.ShouldBe(["compose", "--project-directory", Path.GetFullPath("/srv/stack"), "stop"]);
        _commands.Executed[1].Arguments.ShouldBe(["compose", "--project-directory", Path.GetFullPath("/srv/stack"), "start"]);
    }

    [Fact]
    public async Task Inspect_ReadsTheImageAndWhetherItRuns()
    {
        _commands.Respond(c => c.Arguments.Contains("inspect"), new CommandResult(0, "jellyfin/jellyfin:12.0 false\n", ""));

        var state = await _docker.Inspect("jellyfin", TestContext.Current.CancellationToken);

        state.ShouldBe(new ContainerState("jellyfin/jellyfin:12.0", false));
        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["inspect", "--format", "{{.Config.Image}} {{.State.Running}}", "jellyfin"]);
    }

    [Fact]
    public async Task Inspect_RefusesOutputItCannotRead()
    {
        _commands.Respond(c => c.Arguments.Contains("inspect"), new CommandResult(0, "\n", ""));

        await Should.ThrowAsync<CommandFailedException>(() => _docker.Inspect("jellyfin", TestContext.Current.CancellationToken));
    }
}
