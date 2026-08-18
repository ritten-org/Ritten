using Microsoft.Extensions.DependencyInjection;
using Ritten.Changelogs;
using Ritten.CodeCoverage;
using Ritten.Core;
using Ritten.DotNet;
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
    protected override void ConfigureServices(IServiceCollection services, DotNetToolSettings settings) => services
        .AddChangelogs(settings.Changelog)
        .AddDotNet(settings.Build, settings.Repository)
        .AddCoverage(settings.Coverage)
        .AddGit(settings.Release.TagPrefix)
        .AddNuGet(settings.Release.Feed, settings.Release.Lines)
        .AddGitHubClient()
        .AddBuildReporting();
}
