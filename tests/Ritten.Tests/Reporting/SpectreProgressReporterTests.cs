using Ritten.Contracts;
using Ritten.Reporting;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Ritten.Tests.Reporting;

public class SpectreProgressReporterTests
{
    [Fact]
    public async Task QuietStillShowsTheJobsStructure()
    {
        // Headings, outcomes, and timings are the job's shape, not chatter.
        var console = new TestConsole();
        var sut = new SpectreProgressReporter(console, PipelineLogLevel.Warning);
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
        var sut = new SpectreProgressReporter(console, PipelineLogLevel.Warning);

        sut.Log(PipelineLogLevel.Detail, "Restored everything.");

        console.Output.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailuresRenderTheirErrorsAtEveryLevel()
    {
        var console = new TestConsole();
        var sut = new SpectreProgressReporter(console, PipelineLogLevel.Warning);
        var step = new Step("changelog", StepKind.Check, null, []);

        await sut.OnStepStarted(step, TestContext.Current.CancellationToken);
        await sut.OnStepCompleted(step, StepResult.Failed("The entry is missing."), TestContext.Current.CancellationToken);

        console.Output.ShouldContain("✗");
        console.Output.ShouldContain("The entry is missing.");
    }

    [Theory]
    // --verbose shows everything.
    [InlineData(PipelineLogLevel.Verbose, PipelineLogLevel.Verbose, true)]
    [InlineData(PipelineLogLevel.Verbose, PipelineLogLevel.Error, true)]
    // The default hides diagnostics only.
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Verbose, false)]
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Detail, true)]
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Status, true)]
    // A skipped action is always worth knowing about, but it isn't a failure.
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Skipped, true)]
    [InlineData(PipelineLogLevel.Warning, PipelineLogLevel.Skipped, false)]
    // --quiet keeps warnings and errors.
    [InlineData(PipelineLogLevel.Warning, PipelineLogLevel.Detail, false)]
    [InlineData(PipelineLogLevel.Warning, PipelineLogLevel.Status, false)]
    [InlineData(PipelineLogLevel.Warning, PipelineLogLevel.Warning, true)]
    [InlineData(PipelineLogLevel.Warning, PipelineLogLevel.Error, true)]
    public void IsEnabled_ComparesAgainstTheMinimumLevel(PipelineLogLevel minimum, PipelineLogLevel level, bool expected)
    {
        var sut = new SpectreProgressReporter(AnsiConsole.Console, minimum);

        sut.IsEnabled(level).ShouldBe(expected);
    }

    [Fact]
    public void PipelineLogLevel_AscendsInImportance()
    {
        // The ordering is load-bearing: IsEnabled is a comparison, so reordering these would
        // silently invert which messages get suppressed.
        PipelineLogLevel[] ascending =
        [
            PipelineLogLevel.Verbose,
            PipelineLogLevel.Detail,
            PipelineLogLevel.Status,
            PipelineLogLevel.Skipped,
            PipelineLogLevel.Warning,
            PipelineLogLevel.Error
        ];

        ascending.ShouldBeInOrder(SortDirection.Ascending);
    }
}
