using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Core;
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
        /// Adds <see cref="IChangelog"/> to the service collection.
        /// </summary>
        public IServiceCollection AddChangelogs()
        {
            services.TryAddSingleton<IChangelog, ChangelogClient>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="IDotNet"/> to the service collection.
        /// </summary>
        public IServiceCollection AddDotNet()
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IDotNet, DotNetClient>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="IGit"/> to the service collection.
        /// </summary>
        public IServiceCollection AddGit()
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IGit, GitClient>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="INuGet"/> to the service collection.
        /// </summary>
        public IServiceCollection AddNuGet()
        {
            services.AddCommandRunner();
            services.TryAddSingleton<INuGet, NuGetClient>();
            return services;
        }

        /// <summary>
        /// Registers the services and options that the standard .NET package pipelines need.
        /// </summary>
        public IServiceCollection AddDotNetPackageServices()
        {
            services
                .AddCommandRunner()
                .AddChangelogs()
                .AddDotNet()
                .AddGit()
                .AddNuGet()
                .AddGitHubActionsRuntime()
                .AddBuildReporting();

            if (services.Any(d => d.ServiceType == typeof(DotNetPackageServicesMarker)))
            {
                return services;
            }

            services.AddSingleton<DotNetPackageServicesMarker>();

            services.AddOptions<DotNetOptions>()
                .Configure<RittenProjectFile>((o, file) =>
                {
                    o.Configuration = file.Configuration;
                    o.ProjectFile = file.Project ?? "";
                })
                .Validate(d => !string.IsNullOrEmpty(d.Configuration), "'configuration' in ritten.json must be a build configuration, for example \"Release\".")
                .ValidateOnStart();

            services.AddOptions<ChangelogOptions>()
                .Configure(ChangelogOptions.ConfigureFromEnvironment)
                .Configure<RittenProjectFile>((o, file) =>
                {
                    o.File = file.Changelog;
                    o.RepositoryUrl = file.Repository;
                })
                .Validate(c => !string.IsNullOrEmpty(c.File), "'changelog' in ritten.json must be the path to the changelog, relative to the project root.")
                .ValidateOnStart();

            services.AddOptions<NuGetOptions>()
                .Configure(NuGetOptions.ConfigureFromEnvironment)
                .Configure<RittenProjectFile>((o, file) => o.Feed = file.Feed)
                .Validate(n => !string.IsNullOrEmpty(n.Feed), "'feed' in ritten.json must be the V3 index URL of the feed to publish to.")
                .ValidateOnStart();

            services.AddOptions<GitOptions>()
                .Configure(GitOptions.ConfigureFromEnvironment)
                .Configure<RittenProjectFile>((o, file) => o.TagPrefix = file.TagPrefix);

            return services;
        }

        /// <summary>
        /// Requires a <c>project</c> in <c>ritten.json</c>. Only the pipelines that ship a package
        /// read it, so the verify pipeline doesn't ask for one.
        /// </summary>
        public IServiceCollection RequireDotNetProject()
        {
            if (services.Any(d => d.ServiceType == typeof(DotNetProjectMarker)))
            {
                return services;
            }

            services.AddSingleton<DotNetProjectMarker>();
            services.AddOptions<DotNetOptions>()
                .Validate(d => !string.IsNullOrEmpty(d.ProjectFile), "'project' in ritten.json must be the project file of the package being shipped, relative to the project root.")
                .ValidateOnStart();

            return services;
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

    // Composite registrations can't use TryAdd, because options delegates and enumerable
    // registrations are additive. They key idempotence off a private marker instead of off a
    // service they happen to register, so that a consumer registering their own implementation
    // of one of those services can't silently suppress the rest of the block.
    private sealed class DotNetPackageServicesMarker;

    private sealed class DotNetProjectMarker;

    private sealed class BuildReportingMarker;
}
