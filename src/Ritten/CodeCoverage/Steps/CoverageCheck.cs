using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.CodeCoverage.Steps;

/// <summary>
/// Judges the collected <see cref="Coverage"/> against the configured minimums.
/// </summary>
/// <param name="report">The build report.</param>
[Step("check coverage", StepKind.Check)]
public class CoverageCheck(IWorkflowReport report)
{
    /// <summary>
    /// Judges the collected coverage.
    /// </summary>
    /// <param name="thresholds">The coverage the project requires.</param>
    /// <param name="coverage">The combined coverage the tests produced (see <see cref="ReadCoverage"/>).</param>
    public StepResult Run(CoverageSettings thresholds, Coverage coverage)
    {
        var rows =
            $"- Line coverage: **{coverage.LineRate:0.0}%**{Minimum(thresholds.Line)}\n" +
            $"- Branch coverage: **{coverage.BranchRate:0.0}%**{Minimum(thresholds.Branch)}";

        List<Error> failures = [];
        if (thresholds.Line is { } line && coverage.LineRate < line)
        {
            failures.Add($"Line coverage {coverage.LineRate:0.0}% is below the minimum {line:0.0}%.");
        }

        if (thresholds.Branch is { } branch && coverage.BranchRate < branch)
        {
            failures.Add($"Branch coverage {coverage.BranchRate:0.0}% is below the minimum {branch:0.0}%.");
        }

        if (failures.Count > 0)
        {
            report.Section(SectionName.Coverage).Failure(rows);
            return StepResult.Failed(failures);
        }

        report.Section(SectionName.Coverage).Success(rows);
        return StepResult.Successful;
    }

    private static string Minimum(decimal? minimum) => minimum is { } m ? $" (minimum {m:0.0}%)" : "";
}
