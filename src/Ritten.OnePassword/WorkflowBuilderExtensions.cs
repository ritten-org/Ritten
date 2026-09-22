using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Engine;

namespace Ritten.OnePassword;

/// <summary>
/// Registers 1Password as the workflow's secrets provider.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds 1Password as the <see cref="ISecretProvider"/> the run reads references through.
        /// </summary>
        /// <param name="configure">How the CLI authenticates; omit to rely on the environment or the desktop app.</param>
        public IWorkflowBuilder AddOnePassword(Action<OnePasswordOptions>? configure = null)
        {
            builder.AddCommandRunner();
            var options = builder.Services.AddOptions<OnePasswordOptions>();
            if (configure is not null)
            {
                options.Configure(configure);
            }

            builder.Services.TryAddSingleton<ISecretProvider, OnePasswordSecretProvider>();
            return builder;
        }
    }
}
