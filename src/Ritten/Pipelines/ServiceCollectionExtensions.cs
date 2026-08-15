using Microsoft.Extensions.DependencyInjection;
using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Reporting;

namespace Ritten.Pipelines;

/// <summary>
/// Registers everything the standard pipelines share.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers everything the standard .NET tool pipelines share.
        /// </summary>
        public IServiceCollection AddDotNetToolServices(DotNetToolSettings settings)
        {
            return services
                .AddChangelogs(settings.Changelog)
                .AddDotNet(settings.Build)
                .AddGit(settings.Release.TagPrefix)
                .AddNuGet(settings.Release.Feed, settings.Release.Lines)
                .AddGitHubActionsRuntime()
                .AddBuildReporting();
        }
    }
}
