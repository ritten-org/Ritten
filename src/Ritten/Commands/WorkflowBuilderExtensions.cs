using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Engine;

namespace Ritten.Commands;

/// <summary>
/// Registers the command runner.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="ICommandRunner"/> to the service collection.
        /// </summary>
        public IWorkflowBuilder AddCommandRunner()
        {
            builder.Services.TryAddSingleton<ICommandRunner, CommandRunner>();
            return builder;
        }
    }
}
