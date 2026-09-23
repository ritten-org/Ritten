using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.NuGet.Steps;

/// <summary>
/// Classifies where the project's version stands against the feed.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's NuGet options.</param>
/// <param name="nuget">The NuGet client.</param>
[Step("nuget read", StepKind.Work)]
public class NugetRead(IWorkflowLog log, IOptions<NuGetOptions> options, INuGet nuget)
{
    /// <summary>
    /// Reads the feed and classifies the given project's version.
    /// </summary>
    /// <param name="project">The project being classified (see <see cref="ResolveRelease"/>).</param>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<ReleaseState>> Run(Project project, PackageSet packages, CancellationToken cancellationToken = default)
    {
        var feed = new NuGetFeed(options.Value.Feed);

        // The repository's release history is the union of its packages' histories, so a
        // brand-new package can't blind the version check by having none of its own.
        List<PackagePublication> publications = [];
        List<NuGetVersion> history = [];
        foreach (var package in packages.Packages)
        {
            var versions = await nuget.GetPublishedVersions(feed, package.Name, cancellationToken);
            history.AddRange(versions);
            publications.Add(new PackagePublication(package.Name, versions.Any(v => v == package.Version)));
        }

        var latestPublished = history.DefaultIfEmpty().Max();
        var lineTip = history
            .Where(v => options.Value.Lines.SameLine(v, project.Version))
            .DefaultIfEmpty()
            .Max();

        // A published version can't be ahead of its line's tip, so "latest" means being it; an
        // unpublished version has to beat it. Partially published counts as published here: what
        // remains of it must still ship, wherever the tip stands.
        var anyPublished = history.Any(v => v == project.Version);
        var latestInLine = lineTip is null
            || (anyPublished ? project.Version >= lineTip : project.Version > lineTip);

        // At rest means nothing left to push: every shipped package carries the version.
        var published = publications.Count > 0 && publications.All(p => p.Published);

        var state = new ReleaseState(published, latestInLine, lineTip, latestPublished) { Packages = publications };
        log.Detail(Describe(project, state));
        if (!published && publications.Any(p => p.Published))
        {
            log.Detail($"Already published: {string.Join(", ", publications.Where(p => p.Published).Select(p => p.Name))}; still to push: {string.Join(", ", publications.Where(p => !p.Published).Select(p => p.Name))}.");
        }

        return state;
    }

    private string Describe(Project project, ReleaseState state) => (state.Published, state.LatestInLine) switch
    {
        (true, true) => state.OnLatestLine
            ? $"Version {project.Version} is the latest published version."
            : $"Version {project.Version} is the latest on the {options.Value.Lines.Label(project.Version)} line (latest overall: {state.LatestVersion}).",
        (true, false) => $"Version {project.Version} is published, and {state.LatestVersionInLine} is newer on its line.",
        (false, false) => $"Version {project.Version} is unpublished, and its line has moved on to {state.LatestVersionInLine}.",
        _ => state.LatestVersion == null
            ? $"Version {project.Version} would be the first published version of {project.Name}."
            : project.Version < state.LatestVersion
                ? $"Version {project.Version} is a backport to the {options.Value.Lines.Label(project.Version)} line (latest overall: {state.LatestVersion})."
                : $"Version {project.Version} is unpublished (latest published: {state.LatestVersion})."
    };
}
