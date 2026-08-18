using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Engine;
using Ritten.Engine.Workflows;
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
    protected override void ValidateSettings(SettingsValidator<DotNetPackageSettings> settings) => settings
        .Require(s => s.Build.Project is not null || s.Build.Projects is { Count: > 0 }, "Set 'build.project' (one package) or 'build.projects' (several).")
        .Require(s => s.Build.Project is null || s.Build.Projects is null, "'build.project' and 'build.projects' are both set; use one.");

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<Clean>(),
        Step.FromType<ReadProjects>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<ReadChangelog>(),
        Step.FromType<CheckChangelogLinks>(),
        Step.FromType<NugetRead>(),
        Step.FromType<CheckVersion>(),
        Step.FromType<CheckPackageVersions>(),
        Step.FromType<CheckChangelogEntry>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetFormat>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        .. CoverageSteps.All,
        Step.FromType<DotnetPack>()
    ];
}
