using Ritten.CodeCoverage.Steps;

namespace Ritten.CodeCoverage;

/// <summary>
/// The coverage steps, for jobs to splice after their tests run.
/// </summary>
internal static class CoverageSteps
{
    /// <summary>
    /// Reads the coverage the tests collected, then judges it against the configured minimums.
    /// </summary>
    public static IReadOnlyList<Type> All => [typeof(ReadCoverage), typeof(CoverageValidate)];
}
