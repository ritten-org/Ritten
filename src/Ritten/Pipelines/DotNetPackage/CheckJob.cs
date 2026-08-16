using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet.Steps;
using Ritten.NuGet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNetPackage;

/// <summary>
/// Validates a pull request: formatting, version, changelog, compile, tests, and pack.
/// </summary>
internal sealed class CheckJob : DotNetPackageJob
{
    /// <inheritdoc />
    public override string Name => "check";

    /// <inheritdoc />
    protected override void ValidateSettings(SettingsValidator<DotNetPackageSettings> settings) => settings.Require(s => s.Build.Project);

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<Clean>(),
        Step.FromType<ReadProject>(),
        Step.FromType<ReadChangelog>(),
        Step.FromType<ChangelogLinksValidate>(),
        Step.FromType<NugetRead>(),
        Step.FromType<NugetValidate>(),
        Step.FromType<ChangelogValidate>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormat>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        .. CoverageSteps.All,
        Step.FromType<DotnetPack>()
    ];
}
