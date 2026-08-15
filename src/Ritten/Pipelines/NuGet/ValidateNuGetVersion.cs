using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Fails the pipeline when the project's version is already published, or isn't greater than the
/// latest published version. Requires <see cref="Project"/> in pipeline state
/// (see <see cref="ExtractDotNetProject"/>).
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="state">The pipeline state.</param>
/// <param name="report">The build report.</param>
/// <param name="nuget">The NuGet client.</param>
public class ValidateNuGetVersion(
    IPipelineLog log,
    IOptions<NuGetOptions> options,
    IPipelineState state,
    IBuildReport report,
    INuGet nuget
) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "nuget validate";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.SkipVersionCheck)
        {
            log.Skipped("Skipping version check.");
            return StepResult.Successful;
        }

        var project = state.Get<Project>();
        if (project == null)
        {
            return StepResult.Failed("Project info not found in state.");
        }

        var feed = new NuGetFeed(options.Value.Feed);
        var versions = await nuget.GetPublishedVersions(feed, project.Name, cancellationToken);

        if (versions.Any(v => v == project.Version))
        {
            report.Section("Release")
                .Failure($"Version **{project.Version}** is already published on the feed. Bump `<Version>` in the project file.");
            return StepResult.Failed($"Package version {project.Version} already exists on the feed.");
        }

        var latestVersion = versions.DefaultIfEmpty().Max();
        if (latestVersion != null && project.Version <= latestVersion)
        {
            report.Section("Release")
                .Failure($"Version **{project.Version}** isn't greater than the latest published version **{latestVersion}**. Bump `<Version>` in the project file.");
            return StepResult.Failed($"Project version {project.Version} is not greater than the latest version {latestVersion}.");
        }

        report.Section("Release")
            .Success(latestVersion == null
                ? $"Version **{project.Version}** will be the first published version of {project.Name}."
                : $"Version **{project.Version}** is valid (latest published: **{latestVersion}**).");
        log.Detail($"Version {project.Version} is valid for {project.Name}.");
        return StepResult.Successful;
    }
}
