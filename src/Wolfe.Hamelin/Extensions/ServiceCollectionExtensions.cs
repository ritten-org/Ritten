using Hamelin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;
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
        public IServiceCollection AddCommandRunner() => services.AddSingleton<ICommandRunner, CommandRunner>();

        /// <summary>
        /// Adds <see cref="IChangelog"/> to the service collection.
        /// </summary>
        public IServiceCollection AddChangelogs() => services.AddSingleton<IChangelog, ChangelogClient>();

        /// <summary>
        /// Adds <see cref="IDotNet"/> to the service collection.
        /// </summary>
        public IServiceCollection AddDotNet() => services.AddSingleton<IDotNet, DotNetClient>();

        /// <summary>
        /// Adds <see cref="INuGet"/> to the service collection.
        /// </summary>
        public IServiceCollection AddNuGet() => services.AddSingleton<INuGet, NuGetClient>();

        /// <summary>
        /// Adds <see cref="IBuildReport"/> to the service collection and configures hooks that
        /// publish it to the job summary and pull request when the pipeline finishes.
        /// </summary>
        /// <param name="clientName">The product name used to identify this pipeline to the GitHub API.</param>
        public IServiceCollection AddBuildReporting(string clientName)
        {
            services.AddOptions<GitHubOptions>().BindConfiguration("GitHub");
            services.AddSingleton<IGitHubClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<GitHubOptions>>().Value;
                var client = new GitHubClient(new ProductHeaderValue(clientName));
                if (options.Token is { } token)
                {
                    client.Credentials = new Credentials(token);
                }

                return client;
            });

            services.AddSingleton<IPullRequestCommentService, PullRequestCommentService>();
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
