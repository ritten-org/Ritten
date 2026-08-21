using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// Validates a pull request for a project with no release to prepare.
/// </summary>
internal sealed class CheckJob : DotNetJob
{
    /// <inheritdoc />
    public override string Name => "check";

    /// <inheritdoc />
    public override string Description => "Checks a pull request: formatting, compile, and tests.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Check;

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
