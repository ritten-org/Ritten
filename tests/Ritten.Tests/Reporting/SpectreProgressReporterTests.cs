using Ritten.Contracts;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Tests.Reporting;

public class SpectreProgressReporterTests
{
    [Theory]
    // --verbose shows everything.
    [InlineData(PipelineLogLevel.Verbose, PipelineLogLevel.Verbose, true)]
    [InlineData(PipelineLogLevel.Verbose, PipelineLogLevel.Error, true)]
    // The default hides diagnostics only.
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Verbose, false)]
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Detail, true)]
    [InlineData(PipelineLogLevel.Detail, PipelineLogLevel.Status, true)]
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
            PipelineLogLevel.Warning,
            PipelineLogLevel.Error
        ];

        ascending.ShouldBeInOrder(SortDirection.Ascending);
    }
}
