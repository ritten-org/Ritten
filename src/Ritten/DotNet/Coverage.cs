using System.Xml.Linq;

namespace Ritten.DotNet;

/// <summary>
/// Aggregated code coverage, as raw counters so results combine exactly across test projects.
/// </summary>
public record Coverage
{
    /// <summary>
    /// The lines the tests executed.
    /// </summary>
    public required int LinesCovered { get; init; }

    /// <summary>
    /// The coverable lines.
    /// </summary>
    public required int LinesValid { get; init; }

    /// <summary>
    /// The branches the tests executed.
    /// </summary>
    public required int BranchesCovered { get; init; }

    /// <summary>
    /// The coverable branches.
    /// </summary>
    public required int BranchesValid { get; init; }

    /// <summary>
    /// Line coverage as a percentage; 100 when there is nothing to cover.
    /// </summary>
    public decimal LineRate => Rate(LinesCovered, LinesValid);

    /// <summary>
    /// Branch coverage as a percentage; 100 when there is nothing to cover.
    /// </summary>
    public decimal BranchRate => Rate(BranchesCovered, BranchesValid);

    /// <summary>
    /// Combines two coverage results by summing their counters.
    /// </summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    public static Coverage operator +(Coverage left, Coverage right) => new()
    {
        LinesCovered = left.LinesCovered + right.LinesCovered,
        LinesValid = left.LinesValid + right.LinesValid,
        BranchesCovered = left.BranchesCovered + right.BranchesCovered,
        BranchesValid = left.BranchesValid + right.BranchesValid
    };

    /// <summary>
    /// Reads the counters from the root of a cobertura report.
    /// </summary>
    /// <param name="cobertura">The cobertura XML.</param>
    public static Coverage Parse(Stream cobertura)
    {
        var root = XDocument.Load(cobertura).Root
            ?? throw new InvalidOperationException("The coverage report has no root element.");
        return new Coverage
        {
            LinesCovered = (int?)root.Attribute("lines-covered") ?? 0,
            LinesValid = (int?)root.Attribute("lines-valid") ?? 0,
            BranchesCovered = (int?)root.Attribute("branches-covered") ?? 0,
            BranchesValid = (int?)root.Attribute("branches-valid") ?? 0
        };
    }

    private static decimal Rate(int covered, int valid) => valid == 0 ? 100 : covered * 100m / valid;
}
