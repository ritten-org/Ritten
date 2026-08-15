namespace Ritten.CodeCoverage;

/// <summary>
/// Settings for judging collected coverage.
/// </summary>
public class CoverageOptions
{
    /// <summary>
    /// The minimum line coverage percentage, or <c>null</c> to report without judging.
    /// </summary>
    public decimal? MinimumLine { get; set; }

    /// <summary>
    /// The minimum branch coverage percentage, or <c>null</c> to report without judging.
    /// </summary>
    public decimal? MinimumBranch { get; set; }
}
