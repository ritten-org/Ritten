using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Core;
using Ritten.DotNet.Steps;
using Ritten.Git.Steps;
using Ritten.GitHub.Steps;
using Ritten.NuGet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNetTool;

/// <summary>
/// Validates, packs, tags, creates the GitHub release, and publishes to NuGet.
/// </summary>
internal sealed class DeployJob : DotNetToolJob
{
    /// <inheritdoc />
    public override string Name => "deploy";

    /// <inheritdoc />
    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => settings.Require(s => s.Build.Project);

    /// <inheritdoc />
    protected override IEnumerable<Type> GetSteps() =>
    [
        typeof(Clean),
        typeof(ReadProject),
        typeof(ReadChangelog),
        typeof(ChangelogLinksValidate),
        typeof(NugetRead),
        typeof(NugetValidate),
        typeof(ChangelogValidate),
        typeof(ReleasableGate),
        typeof(DotnetRestore),
        typeof(DotnetBuild),
        typeof(DotnetTest),
        .. CoverageSteps.All,
        typeof(ApprovalGate),
        typeof(NugetAuthenticate),
        typeof(DotnetPack),
        typeof(GitTag),
        typeof(GitHubRelease),
        typeof(NugetPush)
    ];
}
