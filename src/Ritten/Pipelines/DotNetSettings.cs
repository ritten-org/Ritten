using Ritten.Core;

namespace Ritten.Pipelines;

/// <summary>
/// The <c>ritten.json</c> schema for the plain .NET pipeline.
/// </summary>
public sealed record DotNetSettings : PipelineSettings
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
