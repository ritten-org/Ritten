using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Core;
using Ritten.DotNet.Steps;
using Ritten.NuGet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines.DotNetTool;

/// <summary>
/// Validates a pull request: formatting, version, changelog, compile, tests, and pack.
/// </summary>
internal sealed class CheckJob : DotNetToolJob
{
    /// <inheritdoc />
    public override string Name => "check";

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
        typeof(DotnetRestore),
        typeof(DotnetFormat),
        typeof(DotnetBuild),
        typeof(DotnetTest),
        .. CoverageSteps.All,
        typeof(DotnetPack)
    ];
}
