using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

            if (services.Any(d => d.ServiceType == typeof(IConfigureOptions<PipelineOptions>)))
            {
                return services;
            }

            services.AddOptions<PipelineOptions>()
                .BindConfiguration("Pipeline")
                .Validate(p => !string.IsNullOrEmpty(p.ArtifactsDirectory))
                .Validate(p => !string.IsNullOrEmpty(p.TempDirectory))
                .ValidateOnStart();

            services.AddOptions<DotNetOptions>()
                .BindConfiguration("DotNet")
                .Validate(d => !string.IsNullOrEmpty(d.Configuration))
                .Validate(d => !string.IsNullOrEmpty(d.ProjectFile))
                .ValidateOnStart();

            services.AddOptions<ChangelogOptions>()
                .BindConfiguration("Changelog")
                .Validate(c => !string.IsNullOrEmpty(c.File))
                .ValidateOnStart();

            services.AddOptions<NuGetOptions>()
                .BindConfiguration("NuGet")
                .Validate(n => !string.IsNullOrEmpty(n.Feed))
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
            if (services.Any(d => d.ServiceType == typeof(IBuildReport)))
            {
                return services;
            }

            services.AddSingleton<IBuildReport, BuildReport>();
            services.AddSingleton<MarkdownReportRenderer>();
            services.AddSingleton<IProgressReporter, BuildReportPublisher>();
            return services;
        }
    }
}
