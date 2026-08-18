using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.DotNet.Steps;
using Ritten.Git.Steps;
using Ritten.GitHub.Steps;
using Ritten.NuGet.Steps;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNetPackage;

/// <summary>
/// Validates, packs, tags, creates the GitHub release, and publishes to NuGet.
/// </summary>
internal sealed class DeployJob : DotNetPackageJob
{
    /// <inheritdoc />
    public override string Name => "deploy";

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
        Step.FromType<ReleasableGate>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetTest>(),
        .. CoverageSteps.All,
        Step.FromType<ApprovalGate>(),
        Step.FromType<NugetAuthenticate>(),
        Step.FromType<DotnetPack>(),
        Step.FromType<GitTag>(),
        Step.FromType<GitHubRelease>(),
        Step.FromType<NugetPush>()
    ];
}
