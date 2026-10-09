using Ritten.CodeCoverage;
using Ritten.CodeCoverage.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.CodeCoverage;

/// <summary>
/// Without minimums coverage is watched, not enforced, so a project can see its numbers before
/// deciding what to demand of them.
/// </summary>
public class CoverageCheckTests
{
    private static readonly Coverage ThreeQuarters =
        new() { LinesCovered = 75, LinesValid = 100, BranchesCovered = 3, BranchesValid = 4 };

    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _section = new(SectionName.Coverage);
    private CoverageSettings _thresholds = new();

    public CoverageCheckTests()
    {
        _report.Section(SectionName.Coverage).Returns(_section);
    }

    [Fact]
    public void ReportsWithoutJudgingWhenNoMinimumIsSet()
    {
        var result = Step().Run(_thresholds, ThreeQuarters);

        result.IsFailure.ShouldBeFalse();
        _section.Tone.ShouldBe(ReportTone.Success);
        _section.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("75.0%");
    }

    [Fact]
    public void PassesWhenTheMinimumsAreMet()
    {
        _thresholds = new CoverageSettings { Line = 70, Branch = 70 };

        var result = Step().Run(_thresholds, ThreeQuarters);

        result.IsFailure.ShouldBeFalse();
        _section.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("minimum 70.0%");
    }

    [Fact]
    public void FailsWhenLineCoverageIsBelowTheMinimum()
    {
        _thresholds = new CoverageSettings { Line = 80 };

        var result = Step().Run(_thresholds, ThreeQuarters);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem()
            .Message.ShouldBe("Line coverage 75.0% is below the minimum 80.0%.");
        _section.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void ReportsEveryUnmetMinimumAtOnce()
    {
        _thresholds = new CoverageSettings { Line = 80, Branch = 80 };

        var result = Step().Run(_thresholds, ThreeQuarters);

        result.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    private CoverageCheck Step() => new(_report);
}
