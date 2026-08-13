using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Reporting.Sinks;

namespace Ritten.Runtimes.GitHubActions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubActionsRuntime(this IServiceCollection services)
    {
        services.TryAddSingleton<IGitHubActionsRuntime, GitHubActionsRuntime>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportSink, GitHubReportSink>());
        return services;
    }
}
