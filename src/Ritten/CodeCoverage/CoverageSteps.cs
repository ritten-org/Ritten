using Ritten.CodeCoverage.Steps;
using Ritten.Contracts;

namespace Ritten.CodeCoverage;

/// <summary>
/// The coverage steps, for jobs to splice after their tests run.
/// </summary>
internal static class CoverageSteps
{
    /// <summary>
    /// Reads the coverage the tests collected, then judges it against the configured minimums.
    /// </summary>
    public static IReadOnlyList<Step> All => [Step.FromType<ReadCoverage>(), Step.FromType<CoverageValidate>()];
}
