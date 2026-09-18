using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;

namespace Ritten.Docker;

/// <summary>
/// Registers the docker domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the docker client and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddDocker()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IDocker, DockerClient>();
            builder.Decorators.Decorate<IDocker, DryRunDocker>();
            return builder;
        }
    }
}
