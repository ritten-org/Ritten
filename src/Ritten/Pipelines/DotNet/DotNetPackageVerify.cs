using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The compile-and-test pipeline: cleans, checks formatting, then restores, builds, and tests.
/// No release validation, for repositories or branches that don't ship.
/// </summary>
public class DotNetPackageVerify : Pipeline
{
    /// <inheritdoc/>>
    public override string Name => "DotNet Package Verify";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder)
    {
        builder.Services.AddDotNetPackageServices();

        builder
            .UseStep<CleanDirectories>()
            .UseStep<DotNetFormatCheck>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>();
    }
}
