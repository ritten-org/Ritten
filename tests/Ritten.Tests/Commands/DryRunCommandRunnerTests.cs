using Ritten.Commands;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Commands;

public class DryRunCommandRunnerTests
{
    private readonly FakeCommandRunner _inner = new();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    private DryRunCommandRunner Runner() => new(_log, _inner);

    [Fact]
    public async Task ACommandThatChangesNothing_Runs()
    {
        _inner.Respond(_ => true, new CommandResult(0, "registry.opentofu.org/hashicorp/random", ""));

        var result = await Runner().Run(Command.Create("tofu").WithArguments("providers").WithEffect(CommandEffect.None),
            TestContext.Current.CancellationToken);

        result.StandardOutput.ShouldBe("registry.opentofu.org/hashicorp/random");
        _inner.Executed.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ACommandNobodyMarked_ChangesSomething_SoOnlySaysItWouldRun()
    {
        var result = await Runner().Run(Command.Create("tofu").WithArguments("providers", "lock", "-platform=linux_amd64").ThrowOnError(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.StandardOutput.ShouldBeEmpty();
        _inner.Executed.ShouldBeEmpty();
        _log.Received().Log(WorkflowLogLevel.Skipped, "Would run `tofu providers lock -platform=linux_amd64`.");
    }

    [Fact]
    public async Task ADescribedCommand_IsSaidInItsWords()
    {
        await Runner().Run(Command.Create("git").WithArguments("push", "origin", "main").Describe("push main to origin"),
            TestContext.Current.CancellationToken);

        _log.Received().Log(WorkflowLogLevel.Skipped, "Would push main to origin.");
    }

    [Fact]
    public async Task ASkippedCommandsRedactedArguments_AreNotSaid()
    {
        await Runner().Run(Command.Create("docker").WithArguments("login", "--password", "s3cret").RedactArguments(),
            TestContext.Current.CancellationToken);

        _log.Received().Log(WorkflowLogLevel.Skipped, "Would run docker.");
    }
}
