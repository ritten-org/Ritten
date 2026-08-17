using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNetPackage;

/// <summary>
/// Compiles and tests, without any release checks.
/// </summary>
internal sealed class BuildJob : DotNetPackageJob
{
    /// <inheritdoc />
    public override string Name => "build";

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
