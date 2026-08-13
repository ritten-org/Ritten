using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Console;
using Ritten.Contracts.Hooks;
using Ritten.Contracts.Runtime;
using Ritten.Runtimes.GitHubActions.Logging;

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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, GitHubActionsConsoleFormatter>());

        if (context.IsCI)
        {
            services.TryAddSingleton<IRuntimeCommands, GitHubActionsCommands>();
            if (options.EnableLogFormatter)
            {
                services.Configure<ConsoleLoggerOptions>(o => o.FormatterName = Constants.FormatterName);
            }

            if (options.EnableLogGrouping)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPreStepHook, StepGroupingPreStepHook>());
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostStepHook, StepGroupingPostStepHook>());
            }
        }
        else
        {
            services.TryAddSingleton<IRuntimeCommands, GitHubActionsCommandsStub>();
        }

        return services;
    }
}
