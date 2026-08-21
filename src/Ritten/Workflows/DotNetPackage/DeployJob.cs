using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Engine.Workflows;
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
    public override string Description => "Checks, packs, tags, creates the GitHub release, and publishes to NuGet.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Publish;

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
        Step.FromType<CheckPackageMetadata>(),
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
