using Hamelin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Changelogs;
using Ritten.Commands;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Pipelines;
using Ritten.Pipelines.DotNet;
using Ritten.Pipelines.Git;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Reporting.Hooks;
using Ritten.Reporting.Sinks;

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
        /// Adds the GitHub API client and services to the service collection:
        /// <see cref="ICommentService"/> and <see cref="IReleaseService"/>.
        /// </summary>
        /// <param name="clientName">The product name used to identify this pipeline to the GitHub API.</param>
        public IServiceCollection AddGitHub(string? clientName = null)
        {
            if (services.All(d => d.ServiceType != typeof(IGitHubClient)))
            {
                services.AddOptions<GitHubOptions>()
                    .BindConfiguration("GitHub")
                    .Configure(o => GitHubEnvironmentDefaults.Apply(o, Environment.GetEnvironmentVariable));
                services.AddSingleton<IGitHubClient>(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<GitHubOptions>>().Value;
                    var client = new GitHubClient(new ProductHeaderValue(options.ClientName));
                    if (options.Token is { } token)
                    {
                        client.Credentials = new Credentials(token);
                    }

                    return client;
                });

                services.TryAddSingleton<ICommentService, CommentService>();
                services.TryAddSingleton<IReleaseService, ReleaseService>();
            }

            if (clientName is not null)
            {
                services.PostConfigure<GitHubOptions>(o => o.ClientName = clientName);
            }

            return services;
        }

        /// <summary>
        /// Adds everything the standard .NET package pipelines need.
        /// </summary>
        public IServiceCollection AddDotNetPackagePipeline()
        {
            services
                .AddCommandRunner()
                .AddChangelogs()
                .AddDotNet()
                .AddGit()
                .AddNuGet()
                .AddGitHub()
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

            services.AddStepsFromAssemblyContaining<CleanDirectories>();
            return services;
        }

        /// <summary>
        /// Adds <see cref="IBuildReport"/> to the service collection and configures hooks that
        /// publish it to the job summary and pull request when the pipeline finishes.
        /// </summary>
        public IServiceCollection AddBuildReporting()
        {
            services.AddGitHub();
            if (services.Any(d => d.ServiceType == typeof(IBuildReport)))
            {
                return services;
            }

            services.AddSingleton<IBuildReport, BuildReport>();
            services.AddSingleton<MarkdownReportRenderer>();
            services.AddSingleton<IReportSink, JobSummarySink>();
            services.AddSingleton<IReportSink, PullRequestCommentSink>();

            services.AddPrePipelineHook<PendingCommentHook>();
            services.AddPostPipelineHook<PublishReportHook>();
            return services;
        }
    }
}
