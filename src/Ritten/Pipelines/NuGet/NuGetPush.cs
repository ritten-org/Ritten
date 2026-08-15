using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Pushes the packed packages to the configured feed. Requires <see cref="PackResult"/> in
/// pipeline state (see <see cref="DotnetPack"/>); uses <see cref="Project"/> for the report
/// when present.
/// </summary>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="nuget">The NuGet client.</param>
/// <param name="report">The build report.</param>
public class NugetPush(
    IOptions<NuGetOptions> options,
    IPipelineState state,
    INuGet nuget,
    IBuildReport report
) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "dotnet nuget push";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (state.Get<PackResult>() is not { } packed)
        {
            return StepResult.Failed("Pack result not found in state.");
        }
        // The key requirement lives in the client, so a dry run doesn't need one to rehearse.
        var feed = new NuGetFeed(options.Value.Feed) { ApiKey = options.Value.ApiKey };

        foreach (var package in packed.Packages)
        {
            await nuget.Push(feed, package, cancellationToken);
        }

        if (state.Get<Project>() is { } project)
        {
            report.Section("Release").Success($"Published **{project.Name} {project.Version}** to NuGet.");
        }

        return StepResult.Successful;
    }
}
