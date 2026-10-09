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
            builder.Services.TryAddScoped<IOpenTofu, OpenTofuClient>();
            builder.Decorators.Decorate<IOpenTofu, DryRunOpenTofu>();
            return builder;
        }
    }
}
