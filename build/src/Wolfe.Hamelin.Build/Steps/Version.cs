using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Wolfe.Hamelin.Build.Helpers;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Reporting;

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

        var projectInfo = context.State.Get<ProjectInfo>();
        if (projectInfo == null)
        {
            throw new Exception("Project info not found in state.");
        }

        PackageSourceCredential credentials = new(options.Value.NuGetFeed, "dummy", "", true, null);
        var packageSource = new PackageSource(options.Value.NuGetFeed) { Credentials = credentials };
        var repository = Repository.Factory.GetCoreV3(packageSource);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var versions = (await resource!.GetAllVersionsAsync(
            projectInfo.Name,
            new SourceCacheContext(),
            new NuGetLoggerAdapter(logger),
            cancellationToken
        )).ToList();

        if (versions.Any(v => v == projectInfo.Version))
        {
            report.Section("Release").Failure(
                $"Version **{projectInfo.Version}** is already published on the feed — bump `<Version>` in `{options.Value.ProjectFile}`.");
            throw new Exception($"Package version {projectInfo.Version} already exists on the feed.");
        }

        var latestVersion = versions.DefaultIfEmpty().Max();
        if (latestVersion != null && projectInfo.Version <= latestVersion)
        {
            report.Section("Release").Failure(
                $"Version **{projectInfo.Version}** isn't greater than the latest published version **{latestVersion}** — bump `<Version>` in `{options.Value.ProjectFile}`.");
            throw new Exception($"Project version {projectInfo.Version} is not greater than the latest version {latestVersion}.");
        }

        report.Section("Release").Success(
            latestVersion == null
                ? $"Version **{projectInfo.Version}** will be the first published version of {projectInfo.Name}."
                : $"Version **{projectInfo.Version}** is valid (latest published: **{latestVersion}**).");
        logger.LogInformation("Version {Version} is valid and can be used for package {PackageName}.", projectInfo.Version, projectInfo.Name);
    }
}
