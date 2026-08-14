using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Core.Settings;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.NuGet;
using Ritten.Pipelines;
using Ritten.Pipelines.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="ICommandRunner"/> to the service collection.
        /// </summary>
        public IServiceCollection AddCommandRunner()
        {
            services.TryAddSingleton<ICommandRunner, CommandRunner>();
            return services;
        }

        /// <summary>
        /// Adds changelog validation.
        /// </summary>
        public IServiceCollection AddChangelogs(ChangelogSettings settings)
        {
            services.TryAddSingleton<IChangelog, ChangelogClient>();
            services.Configure<ChangelogOptions>(o =>
            {
                o.File = settings.File;
                o.RepositoryUrl = settings.Repository;
            });
            services.Configure<ChangelogOptions>(ChangelogOptions.ConfigureFromEnvironment);
            return services;
        }

        /// <summary>
        /// Adds the .NET client and build settings, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddDotNet(DotNetBuildSettings settings)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IDotNet, DotNetClient>();
            services.Configure<DotNetOptions>(o =>
            {
                o.Configuration = settings.Configuration;
                o.ProjectFile = settings.Project ?? "";
            });
            return services;
        }

        /// <summary>
        /// Adds release tagging, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddGit(string tagPrefix)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IGit, GitClient>();
            services.Configure<GitOptions>(o => o.TagPrefix = tagPrefix);
            services.Configure<GitOptions>(GitOptions.ConfigureFromEnvironment);
            return services;
        }

        /// <summary>
        /// Adds NuGet publishing, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddNuGet(string feed)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<INuGet, NuGetClient>();
            services.Configure<NuGetOptions>(o => o.Feed = feed);
            services.Configure<NuGetOptions>(NuGetOptions.ConfigureFromEnvironment);
            return services;
        }

        /// <summary>
        /// Registers everything the standard .NET package pipelines share.
        /// </summary>
        public IServiceCollection AddDotNetPackageServices(DotNetPackageSettings settings)
        {
            return services
                .AddChangelogs(settings.Changelog)
                .AddDotNet(settings.Build)
                .AddGit(settings.Release.TagPrefix)
                .AddNuGet(settings.Release.Feed)
                .AddGitHubActionsRuntime()
                .AddBuildReporting();
        }

        /// <summary>
        /// Adds <see cref="IBuildReport"/> to the service collection and registers the
        /// <see cref="BuildReportPublisher"/> that publishes it when the pipeline finishes.
        /// </summary>
        public IServiceCollection AddBuildReporting()
        {
            services.AddGitHubActionsRuntime();
            if (services.Any(d => d.ServiceType == typeof(BuildReportingMarker)))
            {
                return services;
            }

            services.AddSingleton<BuildReportingMarker>();
            services.AddSingleton<IBuildReport, BuildReport>();
            services.AddSingleton<MarkdownReportRenderer>();
            services.AddSingleton<IProgressReporter, BuildReportPublisher>();
            return services;
        }
    }

    // Enumerable registrations are additive, so this one can't use TryAdd. It keys idempotence
    // off a private marker rather than off a service it happens to register, so that a consumer
    // supplying their own implementation can't silently suppress the rest of the block.
    private sealed class BuildReportingMarker;
}
