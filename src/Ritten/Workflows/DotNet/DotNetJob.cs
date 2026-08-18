using Ritten.CodeCoverage;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// What every plain .NET job shares.
/// </summary>
internal abstract class DotNetJob : Job<DotNetSettings>
{
    /// <inheritdoc />
    protected override void Configure(WorkflowRunBuilder builder, DotNetSettings settings) => builder
        .AddDotNet(settings.Build)
        .AddCoverage(settings.Coverage)
        .AddBuildReporting();
}
