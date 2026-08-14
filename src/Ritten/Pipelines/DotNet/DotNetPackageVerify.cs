using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The compile-and-test pipeline.
/// </summary>
public class DotNetPackageVerify : Pipeline<DotNetPackageSettings>
{
    /// <inheritdoc/>
    public override string Name => "DotNet Package Verify";

    /// <inheritdoc />
    public override void Configure(IPipelineBuilder builder, DotNetPackageSettings settings)
    {
        builder.Services
            .AddDotNet(settings)
            .AddBuildReporting();

        builder
            .UseStep<CleanDirectories>()
            .UseStep<DotNetFormatCheck>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>();
    }
}
