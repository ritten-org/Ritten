using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Releases;

namespace Ritten.NuGet.Steps;

/// <summary>
/// Classifies where the project's version stands against the feed.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="nuget">The NuGet client.</param>
[Step("release state", StepKind.Work)]
public class DetermineReleaseState(IPipelineLog log, IOptions<NuGetOptions> options, INuGet nuget)
{
    /// <summary>
    /// Reads the feed and classifies the given project's version.
    /// </summary>
    /// <param name="project">The project being classified (see <see cref="ReadProject"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<ReleaseState>> Run(Project project, CancellationToken cancellationToken = default)
    {
        var feed = new NuGetFeed(options.Value.Feed);
        var publishedVersions = await nuget.GetPublishedVersions(feed, project.Name, cancellationToken);
        var latestPublished = publishedVersions.DefaultIfEmpty().Max();
        var lineTip = publishedVersions
            .Where(v => options.Value.Lines.SameLine(v, project.Version))
            .DefaultIfEmpty()
            .Max();

        var published = publishedVersions.Any(v => v == project.Version);

        // A published version can't be ahead of its line's tip, so "latest" means being it;
        // an unpublished version has to beat it.
        var latestInLine = lineTip is null
            || (published ? project.Version >= lineTip : project.Version > lineTip);

        var state = new ReleaseState(published, latestInLine, lineTip, latestPublished);
        log.Detail(Describe(project, state));
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
