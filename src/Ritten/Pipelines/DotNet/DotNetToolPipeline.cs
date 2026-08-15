using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.GitHub;
using Ritten.Pipelines.NuGet;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Pipelines.DotNet;

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

        builder.AddJob("build", job => job
            .UseStep<Clean>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetFormat>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>());

        builder.AddJob("check", job => job
            .Requires(settings.Build.Project)
            .UseStep<Clean>()
            .UseStep<ReadProject>()
            .UseStep<ReadChangelog>()
            .UseStep<NugetValidate>()
            .UseStep<ChangelogValidate>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetFormat>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>()
            .UseStep<DotnetPack>());

        builder.AddJob("deploy", job => job
            .Requires(settings.Build.Project)
            .RequiresEnvironment(RittenEnvironment.NuGetApiKey)
            .RequiresEnvironment(GitHubEnvironment.RepositoryId)
            .UseStep<Clean>()
            .UseStep<ReadProject>()
            .UseStep<ReadChangelog>()
            .UseStep<NugetValidate>()
            .UseStep<ChangelogValidate>()
            .UseStep<ReleasableGate>()
            .UseStep<DotnetRestore>()
            .UseStep<DotnetBuild>()
            .UseStep<DotnetTest>()
            .UseStep<ApprovalGate>()
            .UseStep<DotnetPack>()
            .UseStep<GitTag>()
            .UseStep<GitHubRelease>()
            .UseStep<NugetPush>());
    }
}
