using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.NuGet.Steps;

/// <summary>
/// Pushes the packed packages to the configured feed.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="nuget">The NuGet client.</param>
/// <param name="report">The build report.</param>
[Step("nuget push", StepKind.Publish)]
public class NugetPush(IWorkflowLog log, INuGet nuget, IWorkflowReport report)
{
    /// <summary>
    /// Pushes the packed packages.
    /// </summary>
    /// <param name="feed">The authenticated feed to push to (see <see cref="NugetAuthenticate"/>).</param>
    /// <param name="packed">The packages to push (see <see cref="DotnetPack"/>).</param>
    /// <param name="project">The project being released, when one has been read, for the report.</param>
    /// <param name="release">The release state, carrying which packages are already up.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NuGetFeed feed, PackResult packed, Project project, ReleaseState release, CancellationToken cancellationToken = default)
    {
        // Matched by exact file name, so one package's name can't shadow another's that shares
        // it as a prefix.
        var publications = release.Packages
            .ToDictionary(p => $"{p.Name}.{project.Version}.nupkg", StringComparer.OrdinalIgnoreCase);

        List<string> pushed = [];
        foreach (var package in packed.Packages)
        {
            var publication = publications.GetValueOrDefault(package.Name);
            if (publication is { Published: true })
            {
                log.Skipped($"{publication.Name} is already on the feed.");
                continue;
            }

            await nuget.Push(feed, package, cancellationToken);
            pushed.Add(publication?.Name ?? package.Name);
        }

        // The report tells what actually went up, not what the plan said should.
        report.Section("Release")
            .Success($"Published **{string.Join(", ", pushed)} {project.Version}** to NuGet.");

        return StepResult.Successful;
    }
}
