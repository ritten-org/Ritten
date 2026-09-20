using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// Adds the docker client, its rehearsal, and what the docker steps work on.
        /// </summary>
        /// <param name="images">The images the component builds from its own source.</param>
        public IWorkflowBuilder AddDocker(IReadOnlyList<DockerImage> images)
        {
            builder.AddDocker();
            builder.Services.AddOptions<DockerOptions>().Configure(options => options.Images = images);
            return builder;
        }
    }
}
