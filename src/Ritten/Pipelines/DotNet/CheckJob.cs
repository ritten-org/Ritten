using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// Validates a pull request for a project with no release to prepare.
/// </summary>
internal sealed class CheckJob : DotNetJob
{
    /// <inheritdoc />
    public override string Name => "check";

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<Clean>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormat>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        .. CoverageSteps.All
    ];
}
