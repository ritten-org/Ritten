using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Core;

namespace Ritten.Commands;

/// <summary>
/// Registers the command runner.
/// </summary>
public static class WorkflowRunBuilderExtensions
{
    extension(WorkflowRunBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="ICommandRunner"/> to the service collection.
        /// </summary>
        public WorkflowRunBuilder AddCommandRunner()
        {
            builder.Services.TryAddSingleton<ICommandRunner, CommandRunner>();
            return builder;
        }
    }
}
