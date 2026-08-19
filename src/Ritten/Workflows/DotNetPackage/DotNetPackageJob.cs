using Ritten.Changelogs;
using Ritten.CodeCoverage;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Reporting;

namespace Ritten.Workflows.DotNetPackage;

/// <summary>
/// What every .NET package job shares: the standard service registrations.
/// </summary>
internal abstract class DotNetPackageJob : Job<DotNetPackageSettings>
{
    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, DotNetPackageSettings settings) => builder
        .AddChangelogs(settings.Changelog)
        .AddDotNet(settings.Build, settings.Repository)
        .AddCoverage(settings.Coverage)
        .AddGit(settings.Release.TagPrefix)
        .AddNuGet(settings.Release.Feed, settings.Release.Lines)
        .AddGitHubClient()
        .AddBuildReporting();

}
