using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Core;
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
        /// Adds NuGet publishing, configured from the project's settings.
        /// </summary>
        public IWorkflowBuilder AddNuGet(string feed, ReleaseLine lines)
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<INuGet, NuGetClient>();
            builder.DryRun.Decorate<INuGet, DryRunNuGet>();
            builder.Services.Configure<NuGetOptions>(o =>
            {
                o.Feed = feed;
                o.Lines = lines;
            });
            builder.Services.Configure<NuGetOptions>(NuGetOptions.ConfigureFromEnvironment);
            return builder;
        }
    }
}
