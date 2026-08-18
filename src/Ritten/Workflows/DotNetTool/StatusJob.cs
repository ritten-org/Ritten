using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.NuGet.Steps;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// Reports where the project stands: version, release state, and changelog.
/// </summary>
internal sealed class StatusJob : DotNetToolJob
{
    /// <inheritdoc />
    public override string Name => "status";

    /// <inheritdoc />
    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => settings.Require(s => s.Build.Project);

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ReadProject>(),
        Step.FromType<ReadChangelog>(),
        Step.FromType<NugetRead>(),
        Step.FromType<StatusReport>()
    ];
}
