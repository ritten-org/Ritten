using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;
using Ritten.Releases;

namespace Ritten.NuGet;

/// <summary>
/// Registers the NuGet domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the NuGet client, and the push API key from the environment.
        /// </summary>
        public IWorkflowBuilder AddNuGet()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddScoped<INuGet, NuGetClient>();
            builder.Decorators.Decorate<INuGet, DryRunNuGet>();
            builder.Services.AddOptions<NuGetOptions>().Configure(NuGetOptions.ConfigureFromEnvironment);
            return builder;
        }
    }
}
