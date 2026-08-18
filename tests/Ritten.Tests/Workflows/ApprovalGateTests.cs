using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Workflows.Steps;

namespace Ritten.Tests.Workflows;

public class ApprovalGateTests
{
    private static readonly Project TheProject = new() { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") };

    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly IWorkflowPrompt _prompt = Substitute.For<IWorkflowPrompt>();

    public ApprovalGateTests()
    {
        _prompt.IsInteractive.Returns(true);
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task ProceedsWhenApproved()
    {
        var result = await Step().Run(TheProject, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task StopsWhenDeclined()
    {
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Step().Run(TheProject, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("not approved");
    }

    [Fact]
    public async Task NamesWhatIsBeingReleasedSoDecliningIsInformed()
    {
        await Step().Run(TheProject, TestContext.Current.CancellationToken);

        await _prompt.Received().Confirm(
            Arg.Is<string>(m => m.Contains("My.Package 1.2.0")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallsBackToTheJobNameWhenNoProjectHasBeenRead()
    {
        await Step().Run(null, TestContext.Current.CancellationToken);

        await _prompt.Received().Confirm(
            Arg.Is<string>(m => m.Contains("deploy")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefusesRatherThanHangingWithNoTerminalToAskAt()
    {
        // A build agent waiting forever for a person is worse than one that won't start.
        _prompt.IsInteractive.Returns(false);

        var result = await Step().Run(TheProject, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--auto-approve");
    }

    [Fact]
    public async Task DoesNotAskWhenApprovedUpFront()
    {
        _prompt.IsInteractive.Returns(false);

        var result = await Step(autoApprove: true).Run(TheProject, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _prompt.DidNotReceive().Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotAskForADryRun()
    {
        // Nothing irreversible is going to happen, so there's nothing to approve.
        _prompt.IsInteractive.Returns(false);

        var result = await Step(dryRun: true).Run(TheProject, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _prompt.DidNotReceive().Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private ApprovalGate Step(bool dryRun = false, bool autoApprove = false) =>
        new(new WorkflowJob("Test", "deploy", dryRun, autoApprove), _log, _prompt);
}
