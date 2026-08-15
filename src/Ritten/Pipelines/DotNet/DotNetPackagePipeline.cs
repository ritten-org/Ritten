using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.GitHub;
using Ritten.Pipelines.NuGet;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// Everything Ritten does for a .NET package, as three jobs:
/// <list type="bullet">
/// <item><c>verify</c> — compiles and tests, with no release validation, for branches that don't ship.</item>
/// <item><c>build</c> — the pull request job: adds version and changelog validation.</item>
/// <item><c>deploy</c> — packs, tags, creates the GitHub release, and publishes. Every release step
/// skips work a previous run already completed, so failed deploys can be rerun.</item>
/// </list>
/// </summary>
public class DotNetPackagePipeline : Pipeline<DotNetPackageSettings>
{
    /// <inheritdoc/>
    public override string Name => "DotNet Package";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder, DotNetPackageSettings settings)
    {
        builder.Services.AddDotNetPackageServices(settings);

        builder.AddJob("verify", job => job
            .UseStep<Clean>()
            .UseStep<DotnetFormat>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>());

        builder.AddJob("build", job => job
            .Requires(settings.Build.Project)
            .UseStep<Clean>()
            .UseStep<ReadProject>()
            .UseStep<NugetValidate>()
            .UseStep<ChangelogValidate>()
            .UseStep<DotnetFormat>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>());

        builder.AddJob("deploy", job => job
            .Requires(settings.Build.Project)
            .RequiresEnvironment(RittenEnvironment.NuGetApiKey)
            .RequiresEnvironment(GitHubEnvironment.RepositoryId)
            .UseStep<Clean>()
            .UseStep<ReadProject>()
            .UseStep<NugetValidate>()
            .UseStep<ChangelogValidate>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>()
            .UseStep<Approve>()
            .UseStep<DotnetPack>()
            .UseStep<GitTag>()
            .UseStep<GitHubRelease>()
            .UseStep<NugetPush>());
    }
}
