using Ritten.Core;
using Ritten.Extensions;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Pipelines.NuGet;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// The pull request pipeline: cleans, checks formatting, validates the package version and
/// changelog entry, then restores, builds, and tests.
/// </summary>
public class DotNetPackageBuild : Pipeline<DotNetPackageSettings>
{
    /// <inheritdoc/>
    public override string Name => "DotNet Package Build";

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
            .UseStep<DotNetFormatCheck>()
            .UseStep<ExtractDotNetProject>()
            .UseStep<ValidateNuGetVersion>()
            .UseStep<ValidateChangelog>()
            .UseStep<DotNetRestore>()
            .UseStep<DotNetBuild>()
            .UseStep<DotNetTest>();
    }
}
