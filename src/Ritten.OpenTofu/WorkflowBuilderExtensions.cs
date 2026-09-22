using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;

namespace Ritten.OpenTofu;

/// <summary>
/// Registers the OpenTofu domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the OpenTofu client and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddOpenTofu()
        {
            builder.AddCommandRunner();
            builder.Services.AddOptions<OpenTofuOptions>();
            builder.Services.TryAddSingleton<IOpenTofu, OpenTofuClient>();
            builder.Decorators.Decorate<IOpenTofu, DryRunOpenTofu>();
            return builder;
        }

        /// <summary>
        /// Adds the OpenTofu client, its rehearsal, and the root module to run against.
        /// </summary>
        /// <param name="root">The root module, relative to the project, or null for the project itself.</param>
        public IWorkflowBuilder AddOpenTofu(string? root)
        {
            builder.AddOpenTofu();
            builder.Services.AddOptions<OpenTofuOptions>().Configure(options => options.Root = root);
            return builder;
        }
    }
}
