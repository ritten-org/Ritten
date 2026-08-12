using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Extensions;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Validate Package Version")]
public class Version(
    ILogger<Version> logger,
    IOptions<BuildOptions> options,
    IPipelineContext context,
    IBuildReport report
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.SkipVersion)
        {
            logger.LogInformation("Skipping version check.");
            return;
        }

        var project = context.State.Get<Project>();
        if (project == null)
        {
            throw new Exception("Project info not found in state.");
        }

        PackageSourceCredential credentials = new(options.Value.NuGetFeed, "dummy", "", true, null);
        var packageSource = new PackageSource(options.Value.NuGetFeed) { Credentials = credentials };
        var repository = Repository.Factory.GetCoreV3(packageSource);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var versions = (await resource!.GetAllVersionsAsync(
            project.Name,
            new SourceCacheContext(),
            logger.ForNuGet(),
            cancellationToken
        )).ToList();

        if (versions.Any(v => v == project.Version))
        {
            report.Section("Release").Failure(
                $"Version **{project.Version}** is already published on the feed — bump `<Version>` in `{options.Value.ProjectFile}`.");
            throw new Exception($"Package version {project.Version} already exists on the feed.");
        }

        var latestVersion = versions.DefaultIfEmpty().Max();
        if (latestVersion != null && project.Version <= latestVersion)
        {
            report.Section("Release").Failure(
                $"Version **{project.Version}** isn't greater than the latest published version **{latestVersion}** — bump `<Version>` in `{options.Value.ProjectFile}`.");
            throw new Exception($"Project version {project.Version} is not greater than the latest version {latestVersion}.");
        }

        report.Section("Release").Success(
            latestVersion == null
                ? $"Version **{project.Version}** will be the first published version of {project.Name}."
                : $"Version **{project.Version}** is valid (latest published: **{latestVersion}**).");
        logger.LogInformation("Version {Version} is valid and can be used for package {PackageName}.", project.Version, project.Name);
    }
}
