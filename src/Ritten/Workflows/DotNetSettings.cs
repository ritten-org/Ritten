using Ritten.Engine.Workflows;

namespace Ritten.Workflows;

/// <summary>
/// The <c>ritten.json</c> schema for the plain .NET workflow.
/// </summary>
public sealed record DotNetSettings : WorkflowSettings
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
