using Ritten.Changelogs.Steps;
using Ritten.Core;
using Ritten.DotNet.Steps;
using Ritten.NuGet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNetPackage;

/// <summary>
/// Reports where the project stands: version, release state, and changelog.
/// </summary>
internal sealed class StatusJob : DotNetPackageJob
{
    /// <inheritdoc />
    public override string Name => "status";

    /// <inheritdoc />
    protected override void ValidateSettings(SettingsValidator<DotNetPackageSettings> settings) => settings.Require(s => s.Build.Project);

    /// <inheritdoc />
    protected override IEnumerable<Type> GetSteps() =>
    [
        typeof(ReadProject),
        typeof(ReadChangelog),
        typeof(NugetRead),
        typeof(StatusReport)
    ];
}
