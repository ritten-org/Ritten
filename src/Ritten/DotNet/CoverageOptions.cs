namespace Ritten.DotNet;

/// <summary>
/// Settings for coverage collection and its thresholds.
/// </summary>
public class CoverageOptions
{
    /// <summary>
    /// Whether tests collect coverage at all.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The minimum line coverage percentage, or <c>null</c> to report without judging.
    /// </summary>
    public decimal? MinimumLine { get; set; }

    /// <summary>
    /// The minimum branch coverage percentage, or <c>null</c> to report without judging.
    /// </summary>
    public decimal? MinimumBranch { get; set; }
}
