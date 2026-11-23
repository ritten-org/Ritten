using Ritten.Commands;
using Ritten.Contracts;
using Ritten.OpenTofu;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.OpenTofu;

// What a dry run's command runner lets the OpenTofu client do: initialise, check and plan, but never apply.
public class OpenTofuDryRunTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    [Fact]
    public async Task ItPlans_ButAppliesNothing()
    {
        var tofu = new OpenTofuClient(new DryRunCommandRunner(_log, _commands), Substitute.For<ISecretProvider>());

        await tofu.Init(TofuModule.Project, ct: TestContext.Current.CancellationToken);
        await tofu.VerifyFormatting(TofuModule.Project, TestContext.Current.CancellationToken);
        await tofu.Plan(TofuModule.Project, ct: TestContext.Current.CancellationToken);
        await tofu.Apply(TofuModule.Project, ct: TestContext.Current.CancellationToken);

        _commands.Executed.Select(command => command.Arguments[0]).ShouldBe(["init", "fmt", "plan"]);
        _log.Received().Log(WorkflowLogLevel.Skipped, "Would apply the plan.");
    }
}
