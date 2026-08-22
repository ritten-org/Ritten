using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNetPackage;

/// <summary>
/// Compiles and tests, without any release checks.
/// </summary>
internal sealed class BuildJob : DotNetPackageJob
{
    /// <inheritdoc />
    public override string Name => "build";

    /// <inheritdoc />
    public override string Description => "Compiles and tests, without any release checks.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Work;

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<Clean>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormatCheck>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        .. CoverageSteps.All
    ];
}
