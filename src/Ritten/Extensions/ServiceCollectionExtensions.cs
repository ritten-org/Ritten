using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.Contracts;
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

            services.AddOptions<PipelineOptions>()
                .BindConfiguration("Pipeline")
                .Validate(p => !string.IsNullOrEmpty(p.ArtifactsDirectory), "Pipeline:ArtifactsDirectory must be set to the directory build artifacts are written to, relative to the repository root.")
                .Validate(p => !string.IsNullOrEmpty(p.TempDirectory), "Pipeline:TempDirectory must be set to the directory intermediate pipeline output is written to, relative to the repository root.")
                .ValidateOnStart();

            services.AddOptions<DotNetOptions>()
                .BindConfiguration("DotNet")
                .Validate(d => !string.IsNullOrEmpty(d.Configuration), "DotNet:Configuration must be set to the build configuration to use, for example 'Release'.")
                .Validate(d => !string.IsNullOrEmpty(d.ProjectFile), "DotNet:ProjectFile must be set to the project file of the package being shipped, relative to the repository root.")
                .ValidateOnStart();

            services.AddOptions<ChangelogOptions>()
                .BindConfiguration("Changelog")
                .Validate(c => !string.IsNullOrEmpty(c.File), "Changelog:File must be set to the changelog file, relative to the repository root.")
                .ValidateOnStart();

            services.AddOptions<NuGetOptions>()
                .BindConfiguration("NuGet")
                .Validate(n => !string.IsNullOrEmpty(n.Feed), "NuGet:Feed must be set to the V3 index URL of the feed to publish to.")
                .ValidateOnStart();

            services.AddOptions<GitOptions>()
                .BindConfiguration("Git");

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

    private sealed class BuildReportingMarker;
}
