using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Determines the project's <see cref="ReleaseState"/> against the NuGet feed.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="report">The build report.</param>
/// <param name="nuget">The NuGet client.</param>
public class NugetValidate(
    IPipelineLog log,
    IOptions<NuGetOptions> options,
    IBuildReport report,
    INuGet nuget
) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "nuget validate";

    /// <inheritdoc />
    public StepKind Kind => StepKind.Validation;

    /// <summary>
    /// Determines the release state of the given project's version against the feed.
    /// </summary>
    /// <param name="project">The project being validated (see <see cref="ReadProject"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<ReleaseState>> Run(Project project, CancellationToken cancellationToken = default)
    {
        var feed = new NuGetFeed(options.Value.Feed);
        var publishedVersions = await nuget.GetPublishedVersions(feed, project.Name, cancellationToken);
        var latestPublished = publishedVersions.DefaultIfEmpty().Max();
        var latestInLine = publishedVersions.Where(v => SameLine(v, project.Version)).DefaultIfEmpty().Max();

        // Name the line only when it isn't the whole story; single-line projects stay unqualified.
        var line = latestInLine == latestPublished ? "" : $" on the {LineLabel(project.Version)} line";

        if (publishedVersions.Any(v => v == project.Version) && latestInLine is not null && latestPublished is not null)
        {
            // A published version can't be ahead of its line's latest.
            if (project.Version < latestInLine)
            {
                report.Section("Release")
                    .Failure($"Version **{project.Version}** is already published, and **{latestInLine}** is newer{line}. Bump `<Version>` in the project file.");
                return StepResult.Failed($"Version {project.Version} is already published, and {latestInLine} is newer{line}.");
            }

            report.Section("Release")
                .Success(project.Version == latestPublished
                    ? $"Version **{project.Version}** is the latest published version; nothing new to release."
                    : $"Version **{project.Version}** is the latest on the {LineLabel(project.Version)} line; nothing new to release (latest overall: **{latestPublished}**).");
            log.Detail($"Version {project.Version} is already published; the project is at rest.");
            return ReleaseState.LatestInLine(latestInLine, latestPublished);
        }

        if (latestInLine != null && project.Version <= latestInLine)
        {
            report.Section("Release")
                .Failure($"Version **{project.Version}** must be higher than **{latestInLine}**, the latest published version{line}. Bump `<Version>` in the project file.");
            return StepResult.Failed($"Project version {project.Version} must be higher than {latestInLine}, the latest published version{line}.");
        }

        report.Section("Release")
            .Success(latestPublished == null
                ? $"Version **{project.Version}** will be the first published version of {project.Name}."
                : project.Version < latestPublished
                    ? $"Version **{project.Version}** is a backport to the {LineLabel(project.Version)} line (latest overall: **{latestPublished}**)."
                    : $"Version **{project.Version}** is valid (latest published: **{latestPublished}**).");
        log.Detail($"Version {project.Version} is valid for {project.Name}.");
        return ReleaseState.Releasable(latestInLine, latestPublished);
    }

    private bool SameLine(NuGetVersion a, NuGetVersion b) =>
        a.Major == b.Major && (options.Value.Lines == ReleaseLine.Major || a.Minor == b.Minor);

    private string LineLabel(NuGetVersion version) =>
        options.Value.Lines == ReleaseLine.Major ? $"{version.Major}.x" : $"{version.Major}.{version.Minor}.x";
}
