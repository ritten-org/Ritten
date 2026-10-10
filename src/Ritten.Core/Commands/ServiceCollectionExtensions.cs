using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Reporting;

namespace Ritten.Commands;

/// <summary>
/// Registers the command runner with any host's services, inside a workflow application or not.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the command runner, scoped to the unit of work it serves.
        /// </summary>
        public IServiceCollection AddCommandRunner()
        {
            services.AddLogging();
            services.TryAddSingleton<IWorkflowLog, SilentWorkflowLog>();
            services.TryAddScoped<ICommandRunner, CommandRunner>();
            return services;
        }
    }
}
