namespace Ritten.Pipelines;

/// <summary>
/// The <c>coverage</c> section of <c>ritten.json</c>.
/// </summary>
public sealed record CoverageSettings
{
    /// <summary>
    /// The minimum line coverage percentage.
    /// </summary>
    public decimal? Line { get; init; }

    /// <summary>
    /// The minimum branch coverage percentage.
    /// </summary>
    public decimal? Branch { get; init; }
}
