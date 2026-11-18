using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Engine;

namespace Ritten.Bitwarden;

/// <summary>
/// Registers Bitwarden as the workflow's secrets provider.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds Bitwarden as the <see cref="ISecretProvider"/> the run reads <c>bw://</c> references through.
        /// </summary>
        /// <param name="configure">The server, the credentials and the CLI's directory; omit to use the CLI as it is.</param>
        public IWorkflowBuilder AddBitwarden(Action<BitwardenOptions>? configure = null)
        {
            builder.AddCommandRunner();
            var options = builder.Services.AddOptions<BitwardenOptions>();
            if (configure is not null)
            {
                options.Configure(configure);
            }

            builder.Services.TryAddScoped<ISecretProvider, BitwardenSecretProvider>();
            return builder;
        }
    }
}
