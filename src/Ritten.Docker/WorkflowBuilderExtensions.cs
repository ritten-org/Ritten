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
        /// Adds the docker client.
        /// </summary>
        public IWorkflowBuilder AddDocker()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddScoped<IDocker, DockerClient>();
            return builder;
        }
    }
}
