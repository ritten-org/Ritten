using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Docker;
using Ritten.Reporting;

namespace Ritten.Tests.Docker;

public class DryRunDockerTests
{
    private readonly IDocker _inner = Substitute.For<IDocker>();
    private readonly DryRunDocker _docker;

    public DryRunDockerTests() => _docker = new DryRunDocker(Substitute.For<IWorkflowLog>(), _inner);

    [Fact]
    public async Task Exec_RunsAReadInARehearsal()
    {
        var read = new ContainerExec("garage", ["/garage", "layout", "show"]) { IsReadOnly = true };
        _inner.Exec(read, Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "version 3", ""));

        (await _docker.Exec(read, TestContext.Current.CancellationToken)).StandardOutput.ShouldBe("version 3");
    }

    [Fact]
    public async Task Exec_SkipsAnythingElse()
    {
        var result = await _docker.Exec(new ContainerExec("caddy", ["caddy", "reload"]), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        await _inner.DidNotReceive().Exec(Arg.Any<ContainerExec>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComposeImages_ReadsInARehearsal()
    {
        var project = Substitute.For<IDirectory>();
        _inner.ComposeImages(project, null, Arg.Any<CancellationToken>()).Returns(["70739f85ad2e"]);

        (await _docker.ComposeImages(project, ct: TestContext.Current.CancellationToken)).ShouldBe(["70739f85ad2e"]);
    }

    [Fact]
    public async Task RemoveImage_RemovesNothingInARehearsal()
    {
        var kept = await _docker.RemoveImage("1a08d8dfc2b5", TestContext.Current.CancellationToken);

        kept.ShouldBeNull();
        await _inner.DidNotReceive().RemoveImage(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
