using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// Compiles and tests.
/// </summary>
internal sealed class BuildJob : DotNetJob
{
    /// <inheritdoc />
    public override string Name => "build";

    /// <inheritdoc />
    public override string Description => "Compiles and tests.";

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
