using Microsoft.Extensions.DependencyInjection;
using Ritten.Changelogs;
using Ritten.Changelogs.Steps;
using Ritten.CodeCoverage;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Git;
using Ritten.Git.Steps;
using Ritten.GitHub;
using Ritten.GitHub.Steps;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Pipelines.Steps;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Defines the pipeline jobs for building and maintaining NuGet packages.
/// </summary>
public class DotNetPackagePipeline : Pipeline<DotNetPackageSettings>
{
    /// <inheritdoc/>
    public override string Name => "dotnet package";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder, DotNetPackageSettings settings)
    {
        AddServices(builder.Services, settings);

        builder.AddJob("status", job => job
            .Requires(settings.Build.Project)
            .UseStep<ReadProject>()
            .UseStep<ReadChangelog>()
            .UseStep<NugetRead>()
            .UseStep<StatusReport>()
        );

        builder.AddJob("build", job => job
            .UseStep<Clean>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetFormat>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>()
            .UseCoverage()
        );

        builder.AddJob("check", job => job
            .Requires(settings.Build.Project)
            .UseStep<Clean>()
            .UseStep<ReadProject>()
            .UseStep<ReadChangelog>()
            .UseStep<ChangelogLinksValidate>()
            .UseStep<NugetRead>()
            .UseStep<NugetValidate>()
            .UseStep<ChangelogValidate>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetFormat>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>()
            .UseCoverage()
            .UseStep<DotnetPack>()
        );

        builder.AddJob("deploy", job => job
            .Requires(settings.Build.Project)
            .UseStep<Clean>()
            .UseStep<ReadProject>()
            .UseStep<ReadChangelog>()
            .UseStep<ChangelogLinksValidate>()
            .UseStep<NugetRead>()
            .UseStep<NugetValidate>()
            .UseStep<ChangelogValidate>()
            .UseStep<ReleasableGate>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>()
            .UseCoverage()
            .UseStep<ApprovalGate>()
            .UseStep<NugetAuthenticate>()
            .UseStep<DotnetPack>()
            .UseStep<GitTag>()
            .UseStep<GitHubRelease>()
            .UseStep<NugetPush>()
        );
    }

    private static void AddServices(IServiceCollection services, DotNetPackageSettings settings)
    {
        services
            .AddChangelogs(settings.Changelog)
            .AddDotNet(settings.Build, settings.Repository)
            .AddCoverage(settings.Coverage)
            .AddGit(settings.Release.TagPrefix)
            .AddNuGet(settings.Release.Feed, settings.Release.Lines)
            .AddGitHubActionsRuntime()
            .AddBuildReporting();
    }
}
