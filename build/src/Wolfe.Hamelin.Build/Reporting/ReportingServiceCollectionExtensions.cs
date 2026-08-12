using Hamelin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit;
using Wolfe.Hamelin.Build.Reporting.GitHub;
using Wolfe.Hamelin.Build.Reporting.Hooks;
using Wolfe.Hamelin.Build.Reporting.Sinks;

namespace Wolfe.Hamelin.Build.Reporting;

public static class ReportingServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBuildReporting()
        {
            services.AddOptions<GitHubOptions>().BindConfiguration("GitHub");
            services.AddSingleton<IGitHubClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<GitHubOptions>>().Value;
                var client = new GitHubClient(new ProductHeaderValue("Wolfe.Hamelin.Build"));
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
