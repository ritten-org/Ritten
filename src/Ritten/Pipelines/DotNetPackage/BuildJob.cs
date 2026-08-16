using Ritten.CodeCoverage;
using Ritten.DotNet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNetPackage;

/// <summary>
/// Compiles and tests, without any release validation.
/// </summary>
internal sealed class BuildJob : DotNetPackageJob
{
    /// <inheritdoc />
    public override string Name => "build";

    /// <inheritdoc />
    protected override IEnumerable<Type> GetSteps() =>
    [
        typeof(Clean),
        typeof(DotnetRestore),
        typeof(DotnetFormat),
        typeof(DotnetBuild),
        typeof(DotnetTest),
        .. CoverageSteps.All
    ];
}
