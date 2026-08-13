using System.ComponentModel;
using Ritten.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Fails the pipeline when the project's version is already published, or isn't greater than the
/// latest published version. Requires <see cref="Project"/> in pipeline state
/// (see <see cref="ExtractDotNetProject"/>).
/// </summary>
/// <param name="logger">The step's logger.</param>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="dotnet">The pipeline's .NET options.</param>
/// <param name="context">The pipeline context.</param>
/// <param name="report">The build report.</param>
/// <param name="nuget">The NuGet client.</param>
[DisplayName("Validate NuGet Version")]
public class ValidateNuGetVersion(
    ILogger<ValidateNuGetVersion> logger,
    IOptions<NuGetOptions> options,
    IOptions<DotNetOptions> dotnet,
    IPipelineContext context,
    IBuildReport report,
    INuGet nuget
) : IPipelineStep
{
    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.SkipVersionCheck)
        {
            logger.LogInformation("Skipping version check.");
            return;
        }

        var project = context.State.Get<Project>();
        if (project == null)
        {
            throw new Exception("Project info not found in state.");
        }

        var feed = new NuGetFeed(options.Value.Feed);
        var versions = await nuget.GetPublishedVersions(feed, project.Name, cancellationToken);

        if (versions.Any(v => v == project.Version))
        {
            report.Section("Release")
                .Failure($"Version **{project.Version}** is already published on the feed — bump `<Version>` in `{dotnet.Value.ProjectFile}`.");
            throw new Exception($"Package version {project.Version} already exists on the feed.");
        }

        var latestVersion = versions.DefaultIfEmpty().Max();
        if (latestVersion != null && project.Version <= latestVersion)
        {
            report.Section("Release")
                .Failure($"Version **{project.Version}** isn't greater than the latest published version **{latestVersion}** — bump `<Version>` in `{dotnet.Value.ProjectFile}`.");
            throw new Exception($"Project version {project.Version} is not greater than the latest version {latestVersion}.");
        }

        report.Section("Release")
            .Success(latestVersion == null
                ? $"Version **{project.Version}** will be the first published version of {project.Name}."
                : $"Version **{project.Version}** is valid (latest published: **{latestVersion}**).");
        logger.LogInformation("Version {Version} is valid and can be used for package {PackageName}.", project.Version, project.Name);
    }
}
