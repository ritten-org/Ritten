using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.GitHub;
using Ritten.Pipelines.NuGet;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The release pipeline: validates the package version and changelog, restores, builds, and tests,
/// then packs, tags, creates the GitHub release, and lastly publishes to the NuGet feed.
/// Every release step skips work a previous run already completed, so failed deploys can be rerun.
/// </summary>
public class DotNetPackageDeploy : Pipeline
{
    /// <inheritdoc/>>
    public override string Name => "DotNet Package Deploy";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.Services.AddDotNetPackageServices();

        builder
            .UseStep<CleanDirectories>()
            .UseStep<ExtractDotNetProject>()
            .UseStep<ValidateNuGetVersion>()
            .UseStep<ValidateChangelog>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>()
            .UseStep<DotNetPack>()
            .UseStep<CreateGitTag>()
            .UseStep<CreateGitHubRelease>()
            .UseStep<NuGetPush>();
    }
}
