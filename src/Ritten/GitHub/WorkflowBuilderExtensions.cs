using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Engine;

namespace Ritten.GitHub;

/// <summary>
/// Provides extension methods for registering the GitHub client.
/// </summary>
internal static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the client the workflow talks to GitHub itself with.
        /// </summary>
        /// <param name="clientName">The product name used to identify this workflow to the GitHub API.</param>
        public IWorkflowBuilder AddGitHubClient(string? clientName = null)
        {
            if (builder.Services.All(d => d.ServiceType != typeof(GitHubClientMarker)))
            {
                builder.Services.AddSingleton<GitHubClientMarker>();

                builder.AddCommandRunner();
                builder.Services.AddOptions<GitHubClientOptions>()
                    .Configure<WorkflowEnvironment>((options, environment) => options.Token = environment.Get(GitHubEnvironment.Token));

                builder.Services.TryAddSingleton<ICredentialStore, AmbientCredentialStore>();
                builder.Services.AddSingleton<IGitHubClient>(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<GitHubClientOptions>>().Value;
                    return new GitHubClient(new ProductHeaderValue(options.ClientName), provider.GetRequiredService<ICredentialStore>());
                });

                builder.Services.TryAddSingleton<IReleaseService, ReleaseService>();
                builder.Decorators.Replace<IReleaseService, DryRunReleaseService>();
            }

            if (clientName is not null)
            {
                builder.Services.PostConfigure<GitHubClientOptions>(o => o.ClientName = clientName);
            }

            return builder;
        }
    }

    /// <summary>
    /// Private marker type so that consumers can't suppress the checks.
    /// </summary>
    private sealed class GitHubClientMarker;
}
