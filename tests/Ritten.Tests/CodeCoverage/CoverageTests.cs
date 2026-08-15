using System.Text;
using Ritten.CodeCoverage;

namespace Ritten.Tests.CodeCoverage;

public class CoverageTests
{
    [Fact]
    public void Parse_ReadsTheCountersFromTheReportRoot()
    {
        var coverage = Parse(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="0.72" branch-rate="0.64" lines-covered="131" lines-valid="462" branches-covered="16" branches-valid="25" version="1.9" timestamp="1755264000">
              <packages />
            </coverage>
            """);

        coverage.LinesCovered.ShouldBe(131);
        coverage.LinesValid.ShouldBe(462);
        coverage.BranchesCovered.ShouldBe(16);
        coverage.BranchesValid.ShouldBe(25);
    }

    [Fact]
    public void Rates_ComeFromTheCountersNotTheReportedRates()
    {
        // Summed counters combine exactly across projects; pre-computed rates don't.
        var coverage = new Coverage { LinesCovered = 3, LinesValid = 4, BranchesCovered = 1, BranchesValid = 2 };

        coverage.LineRate.ShouldBe(75m);
        coverage.BranchRate.ShouldBe(50m);
    }

    [Fact]
    public void NothingToCover_CountsAsFullyCovered()
    {
        var coverage = new Coverage { LinesCovered = 0, LinesValid = 0, BranchesCovered = 0, BranchesValid = 0 };

        coverage.LineRate.ShouldBe(100m);
        coverage.BranchRate.ShouldBe(100m);
    }

    [Fact]
    public void Addition_SumsTheCounters()
    {
        var one = new Coverage { LinesCovered = 1, LinesValid = 2, BranchesCovered = 3, BranchesValid = 4 };
        var two = new Coverage { LinesCovered = 10, LinesValid = 20, BranchesCovered = 30, BranchesValid = 40 };

        var sum = one + two;

        sum.ShouldBe(new Coverage { LinesCovered = 11, LinesValid = 22, BranchesCovered = 33, BranchesValid = 44 });
    }

    private static Coverage Parse(string xml) =>
        Coverage.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml.TrimStart())));
}
