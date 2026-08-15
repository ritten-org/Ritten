using Ritten.Changelogs.Steps;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Git.Steps;
using Ritten.GitHub;
using Ritten.GitHub.Steps;
using Ritten.NuGet.Steps;
using Ritten.Pipelines.Steps;

namespace Ritten.Pipelines;

/// <summary>
/// Defines the pipeline jobs for building and maintaining .NET tools.
/// </summary>
public class DotNetToolPipeline : Pipeline<DotNetToolSettings>
{
    /// <inheritdoc/>
    public override string Name => "dotnet tool";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder, DotNetToolSettings settings)
    {
        builder.Services.AddDotNetToolServices(settings);

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
            .UseCoverage(settings.Coverage)
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
            .UseCoverage(settings.Coverage)
            .UseStep<DotnetPack>()
        );

        builder.AddJob("deploy", job => job
            .Requires(settings.Build.Project)
            .RequiresEnvironment(RittenEnvironment.NuGetApiKey)
            .RequiresEnvironment(GitHubEnvironment.RepositoryId)
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
            .UseCoverage(settings.Coverage)
            .UseStep<ApprovalGate>()
            .UseStep<DotnetPack>()
            .UseStep<GitTag>()
            .UseStep<GitHubRelease>()
            .UseStep<NugetPush>()
        );
    }
}
