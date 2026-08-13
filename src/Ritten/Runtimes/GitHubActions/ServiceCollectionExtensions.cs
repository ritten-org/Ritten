using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Reporting.Sinks;

namespace Ritten.Runtimes.GitHubActions;

/// <summary>
/// Provides extension methods for registering GitHub Actions runtime services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the GitHub Actions runtime services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The product name used to identify this pipeline to the GitHub API.</param>
    public static IServiceCollection AddGitHubActionsRuntime(this IServiceCollection services, string? clientName = null)
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

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportSink, GitHubReportSink>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportSink, GitHubCommentSink>());

        if (clientName is not null)
        {
            services.PostConfigure<GitHubOptions>(o => o.ClientName = clientName);
        }

        return services;
    }
}
