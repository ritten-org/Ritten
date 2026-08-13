using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts.Runtime;

namespace Ritten.Runtimes.GitHubActions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubActionsRuntime(this IServiceCollection services, Action<GitHubActionsRuntimeOptions>? configure = null)
    {
        var options = new GitHubActionsRuntimeOptions();
        configure?.Invoke(options);

        var context = new GitHubActionsContext
        {
            IsCI = options.RuntimeDetector()
        };

        services.TryAddSingleton<IRuntimeContext>(context);

        return services;
    }
}
