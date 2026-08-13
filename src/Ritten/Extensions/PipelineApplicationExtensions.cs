using Ritten.Core;
using Ritten.Pipelines;
using Ritten.Pipelines.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.GitHub;
using Ritten.Pipelines.NuGet;

namespace Ritten.Extensions;

/// <summary>
/// Standard pipeline compositions for repositories that ship a .NET package with a Keep a Changelog file.
/// Each preset appends the steps <see cref="ServiceCollectionExtensions.AddDotNetPackagePipeline"/>
/// registers; interleave repository-specific steps with <see cref="RittenApplication.UseStep{T}"/>.
/// </summary>
public static class RittenApplicationExtensions
{
    extension(RittenApplication pipeline)
    {
        /// <summary>
        /// The pull request pipeline: cleans, checks formatting, validates the package version and
        /// changelog entry, then restores, builds, and tests.
        /// </summary>
        public RittenApplication UseDotNetPackageBuild() => pipeline
            .UseStep<CleanDirectories>()
            .UseStep<DotNetFormatCheck>()
            .UseStep<ExtractDotNetProject>()
            .UseStep<ValidateNuGetVersion>()
            .UseStep<ValidateChangelog>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>();

        /// <summary>
        /// The compile-and-test pipeline: cleans, checks formatting, then restores, builds, and
        /// tests — no release validation, for repositories or branches that don't ship.
        /// </summary>
        public RittenApplication UseDotNetPackageVerify() => pipeline
            .UseStep<CleanDirectories>()
            .UseStep<DotNetFormatCheck>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>();

        /// <summary>
        /// The release pipeline: everything <see cref="UseDotNetPackageBuild"/> validates (except
        /// formatting), then packs, tags, creates the GitHub release, and publishes to the feed.
        /// Every release step skips work a previous run already completed, so failed deploys can
        /// be rerun.
        /// </summary>
        public RittenApplication UseDotNetPackageDeploy() => pipeline
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
