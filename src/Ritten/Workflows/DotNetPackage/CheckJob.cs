using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.DotNet.Steps;
using Ritten.NuGet.Steps;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNetPackage;

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
        Step.FromType<CheckChangelogLinks>(),
        Step.FromType<NugetRead>(),
        Step.FromType<CheckVersion>(),
        Step.FromType<CheckChangelogEntry>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormat>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        .. CoverageSteps.All,
        Step.FromType<DotnetPack>()
    ];
}
