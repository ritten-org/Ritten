using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;

namespace Ritten.DotNet;

/// <summary>
/// Registers the .NET domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the .NET client.
        /// </summary>
        public IWorkflowBuilder AddDotNet()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddScoped<IDotNet, DotNetClient>();
            builder.Decorators.Decorate<IDotNet, DryRunDotNet>();
            return builder;
        }
    }
}
