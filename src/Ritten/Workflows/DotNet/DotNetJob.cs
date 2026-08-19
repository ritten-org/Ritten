using Ritten.CodeCoverage;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// What every plain .NET job shares.
/// </summary>
internal abstract class DotNetJob : Job<DotNetSettings>
{
    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, DotNetSettings settings) => builder
        .AddDotNet(settings.Build)
        .AddCoverage(settings.Coverage)
        .AddBuildReporting();
}
