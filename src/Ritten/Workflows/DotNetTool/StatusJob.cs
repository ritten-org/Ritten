using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
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
    public override string Description => "Reports where the project stands: version, release state, and changelog.";

    /// <inheritdoc />
    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => settings
        .Require(s => s.Build.Project is not null || s.Build.Projects is { Count: > 0 }, "Set 'build.project' (one package) or 'build.projects' (several).")
        .Require(s => s.Build.Project is null || s.Build.Projects is null, "'build.project' and 'build.projects' are both set; use one.");

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ReadProjects>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<ReadChangelog>(),
        Step.FromType<NugetRead>(),
        Step.FromType<StatusReport>()
    ];
}
