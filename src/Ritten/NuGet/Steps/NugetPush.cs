using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.NuGet.Steps;

/// <summary>
/// Pushes the packed packages to the configured feed.
/// </summary>
/// <param name="nuget">The NuGet client.</param>
/// <param name="report">The build report.</param>
[Step("nuget push", StepKind.Publish)]
public class NugetPush(INuGet nuget, IBuildReport report)
{
    /// <summary>
    /// Pushes the packed packages.
    /// </summary>
    /// <param name="feed">The authenticated feed to push to (see <see cref="NugetAuthenticate"/>).</param>
    /// <param name="packed">The packages to push (see <see cref="DotnetPack"/>).</param>
    /// <param name="project">The project being released, when one has been read, for the report.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NuGetFeed feed, PackResult packed, Project project, CancellationToken cancellationToken = default)
    {
        foreach (var package in packed.Packages)
        {
            await nuget.Push(feed, package, cancellationToken);
        }

        report.Section("Release")
            .Success($"Published **{project.Name} {project.Version}** to NuGet.");

        return StepResult.Successful;
    }
}
