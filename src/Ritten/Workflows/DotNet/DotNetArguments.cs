using Ritten.CodeCoverage;
using Ritten.DotNet;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// What the plain .NET workflow's jobs run with: its <c>ritten.json</c> schema.
/// </summary>
public record DotNetArguments
{
    /// <summary>
    /// What to build, and how.
    /// </summary>
    public DotNetBuildSettings Build { get; init; } = new();

    /// <summary>
    /// Code coverage collection and thresholds.
    /// </summary>
    public CoverageSettings Coverage { get; init; } = new();
}