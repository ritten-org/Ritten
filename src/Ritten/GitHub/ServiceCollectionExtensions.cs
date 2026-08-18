using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Commands;
using Ritten.Contracts;

namespace Ritten.GitHub;

/// <summary>
/// Provides extension methods for registering the GitHub client.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the client the workflow talks to GitHub itself with.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The product name used to identify this workflow to the GitHub API.</param>
    public static IServiceCollection AddGitHubClient(this IServiceCollection services, string? clientName = null)
    {
        if (services.All(d => d.ServiceType != typeof(GitHubClientMarker)))
        {
            services.AddSingleton<GitHubClientMarker>();

            services.AddCommandRunner();
            services.AddOptions<GitHubClientOptions>()
                .Configure<WorkflowEnvironment>((options, environment) => options.Token = environment.Get(GitHubEnvironment.Token));

            services.TryAddSingleton<ICredentialStore, AmbientCredentialStore>();
            services.AddSingleton<IGitHubClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value;
                return new GitHubClient(new ProductHeaderValue(options.ClientName), provider.GetRequiredService<ICredentialStore>());
            });

            services.TryAddSingleton<IReleaseService, ReleaseService>();
        }

        if (clientName is not null)
        {
            services.PostConfigure<GitHubClientOptions>(o => o.ClientName = clientName);
        }

        return services;
    }

    /// <summary>
    /// Private marker type so that consumers can't suppress the checks.
    /// </summary>
    private sealed class GitHubClientMarker;
}
