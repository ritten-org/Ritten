using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.GitHub;
using Ritten.Reporting.Sinks;

namespace Ritten.Runtimes.GitHubActions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubActionsRuntime(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportSink, GitHubReportSink>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportSink, GitHubCommentSink>());
        return services;
    }
}
