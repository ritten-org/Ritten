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
public class DotNetPackageDeploy : Pipeline<DotNetPackageSettings>
{
    /// <inheritdoc/>
    public override string Name => "DotNet Package Deploy";

    /// <inheritdoc />
    public override bool TryValidate(DotNetPackageSettings settings, out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrEmpty(settings.Project))
        {
            errors.Add($"'project' not set in {RittenProject.FileName}.");
        }

        return errors.Count == 0;
    }

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder, DotNetPackageSettings settings)
    {
        builder.Services.AddDotNetPackageServices(settings);

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
