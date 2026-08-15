using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Pushes the packed packages to the configured feed.
/// </summary>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="nuget">The NuGet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet nuget push", StepKind.Publish)]
public class NugetPush(IOptions<NuGetOptions> options, INuGet nuget, IBuildReport report)
{
    /// <summary>
    /// Pushes the packed packages.
    /// </summary>
    /// <param name="packed">The packages to push (see <see cref="DotnetPack"/>).</param>
    /// <param name="project">The project being released, when one has been read, for the report.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(PackResult packed, Project project, CancellationToken cancellationToken = default)
    {
        // The key requirement lives in the client, so a dry run doesn't need one to rehearse.
        var feed = new NuGetFeed(options.Value.Feed) { ApiKey = options.Value.ApiKey };

        foreach (var package in packed.Packages)
        {
            await nuget.Push(feed, package, cancellationToken);
        }

        report.Section("Release")
            .Success($"Published **{project.Name} {project.Version}** to NuGet.");


        return StepResult.Successful;
    }
}
