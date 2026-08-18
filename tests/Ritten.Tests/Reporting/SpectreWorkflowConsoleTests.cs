using Ritten.Contracts;
using Ritten.Reporting;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Ritten.Tests.Reporting;

public class SpectreWorkflowConsoleTests
{
    [Fact]
    public async Task QuietStillShowsTheJobsStructure()
    {
        // Headings, outcomes, and timings are the job's shape, not chatter.
        var console = new TestConsole();
        var sut = new SpectreWorkflowConsole(console, WorkflowLogLevel.Warning);
        var step = new Step("git tag", StepKind.Publish, null, []);

        await sut.OnStepStarted(step, TestContext.Current.CancellationToken);
        await sut.OnStepCompleted(step, StepResult.Successful, TestContext.Current.CancellationToken);

        console.Output.ShouldContain("git tag");
        console.Output.ShouldContain("✓");
    }

    [Fact]
    public void QuietSilencesWhatStepsSay()
    {
        var console = new TestConsole();
        var sut = new SpectreWorkflowConsole(console, WorkflowLogLevel.Warning);

        sut.Log(WorkflowLogLevel.Detail, "Restored everything.");

        console.Output.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailuresRenderTheirErrorsAtEveryLevel()
    {
        var console = new TestConsole();
        var sut = new SpectreWorkflowConsole(console, WorkflowLogLevel.Warning);
        var step = new Step("changelog", StepKind.Check, null, []);

        await sut.OnStepStarted(step, TestContext.Current.CancellationToken);
        await sut.OnStepCompleted(step, StepResult.Failed("The entry is missing."), TestContext.Current.CancellationToken);

        console.Output.ShouldContain("✗");
        console.Output.ShouldContain("The entry is missing.");
    }

    [Theory]
    // --verbose shows everything.
    [InlineData(WorkflowLogLevel.Verbose, WorkflowLogLevel.Verbose, true)]
    [InlineData(WorkflowLogLevel.Verbose, WorkflowLogLevel.Error, true)]
    // The default hides diagnostics only.
    [InlineData(WorkflowLogLevel.Detail, WorkflowLogLevel.Verbose, false)]
    [InlineData(WorkflowLogLevel.Detail, WorkflowLogLevel.Detail, true)]
    [InlineData(WorkflowLogLevel.Detail, WorkflowLogLevel.Status, true)]
    // A skipped action is always worth knowing about, but it isn't a failure.
    [InlineData(WorkflowLogLevel.Detail, WorkflowLogLevel.Skipped, true)]
    [InlineData(WorkflowLogLevel.Warning, WorkflowLogLevel.Skipped, false)]
    // --quiet keeps warnings and errors.
    [InlineData(WorkflowLogLevel.Warning, WorkflowLogLevel.Detail, false)]
    [InlineData(WorkflowLogLevel.Warning, WorkflowLogLevel.Status, false)]
    [InlineData(WorkflowLogLevel.Warning, WorkflowLogLevel.Warning, true)]
    [InlineData(WorkflowLogLevel.Warning, WorkflowLogLevel.Error, true)]
    public void IsEnabled_ComparesAgainstTheMinimumLevel(WorkflowLogLevel minimum, WorkflowLogLevel level, bool expected)
    {
        var sut = new SpectreWorkflowConsole(AnsiConsole.Console, minimum);

        sut.IsEnabled(level).ShouldBe(expected);
    }

    [Fact]
    public void WorkflowLogLevel_AscendsInImportance()
    {
        // The ordering is load-bearing: IsEnabled is a comparison, so reordering these would
        // silently invert which messages get suppressed.
        WorkflowLogLevel[] ascending =
        [
            WorkflowLogLevel.Verbose,
            WorkflowLogLevel.Detail,
            WorkflowLogLevel.Status,
            WorkflowLogLevel.Skipped,
            WorkflowLogLevel.Warning,
            WorkflowLogLevel.Error
        ];

        ascending.ShouldBeInOrder(SortDirection.Ascending);
    }
}
