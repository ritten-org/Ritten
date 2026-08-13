using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Pipelines.NuGet;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The pull request pipeline: cleans, checks formatting, validates the package version and
/// changelog entry, then restores, builds, and tests.
/// </summary>
public class DotNetPackageBuild : Pipeline
{
    /// <inheritdoc/>>
    public override string Name => "DotNet Package Build";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.Services.AddDotNetPackageServices();

        builder
            .UseStep<CleanDirectories>()
            .UseStep<DotNetFormatCheck>()
            .UseStep<ExtractDotNetProject>()
            .UseStep<ValidateNuGetVersion>()
            .UseStep<ValidateChangelog>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>();
    }
}
