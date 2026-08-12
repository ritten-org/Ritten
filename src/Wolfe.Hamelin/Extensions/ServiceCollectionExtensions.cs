using Hamelin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Octokit;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Git;
using Wolfe.Hamelin.GitHub;
using Wolfe.Hamelin.NuGet;
using Wolfe.Hamelin.Reporting;
using Wolfe.Hamelin.Reporting.Hooks;
using Wolfe.Hamelin.Reporting.Sinks;

namespace Wolfe.Hamelin.Extensions;

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
