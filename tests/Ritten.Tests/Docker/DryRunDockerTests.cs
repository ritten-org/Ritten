using Ritten.Commands;
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
}
