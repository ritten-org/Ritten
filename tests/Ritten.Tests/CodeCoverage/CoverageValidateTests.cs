using Microsoft.Extensions.Options;
using Ritten.CodeCoverage;
using Ritten.CodeCoverage.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.CodeCoverage;

/// <summary>
/// Without minimums coverage is watched, not enforced, so a project can see its numbers before
/// deciding what to demand of them.
/// </summary>
public class CoverageValidateTests
{
    private static readonly Coverage ThreeQuarters =
        new() { LinesCovered = 75, LinesValid = 100, BranchesCovered = 3, BranchesValid = 4 };

    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _section = new("Coverage");
    private readonly CoverageOptions _options = new();

    public CoverageValidateTests()
    {
        _report.Section("Coverage").Returns(_section);
    }

    [Fact]
    public void ReportsWithoutJudgingWhenNoMinimumIsSet()
    {
        var result = Step().Run(ThreeQuarters);

        result.IsFailure.ShouldBeFalse();
        _section.Tone.ShouldBe(ReportTone.Success);
        _section.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("75.0%");
    }

    [Fact]
    public void PassesWhenTheMinimumsAreMet()
    {
        _options.MinimumLine = 70;
        _options.MinimumBranch = 70;

        var result = Step().Run(ThreeQuarters);

        result.IsFailure.ShouldBeFalse();
        _section.Entries.ShouldHaveSingleItem().ToMarkdown().ShouldContain("minimum 70.0%");
    }

    [Fact]
    public void FailsWhenLineCoverageIsBelowTheMinimum()
    {
        _options.MinimumLine = 80;

        var result = Step().Run(ThreeQuarters);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem()
            .Message.ShouldBe("Line coverage 75.0% is below the minimum 80.0%.");
        _section.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void ReportsEveryUnmetMinimumAtOnce()
    {
        _options.MinimumLine = 80;
        _options.MinimumBranch = 80;

        var result = Step().Run(ThreeQuarters);

        result.Errors.ShouldNotBeNull().Count.ShouldBe(2);
    }

    private CoverageValidate Step() => new(Options.Create(_options), _report);
}
