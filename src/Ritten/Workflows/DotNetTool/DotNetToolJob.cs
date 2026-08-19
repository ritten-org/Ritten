using Ritten.Changelogs;
using Ritten.CodeCoverage;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Reporting;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// What every .NET tool job shares: the standard service registrations.
/// </summary>
internal abstract class DotNetToolJob : Job<DotNetToolSettings>
{
    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, DotNetToolSettings settings) => builder
        .AddChangelogs(settings.Changelog)
        .AddDotNet(settings.Build, settings.Repository)
        .AddCoverage(settings.Coverage)
        .AddGit(settings.Release.TagPrefix)
        .AddNuGet(settings.Release.Feed, settings.Release.Lines)
        .AddGitHubClient()
        .AddBuildReporting();
}
