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
        /// Adds NuGet publishing, configured from the project's settings.
        /// </summary>
        /// <param name="feed">The V3 index URL of the feed versions are checked against and pushed to.</param>
        /// <param name="lines">How published versions group into release lines.</param>
        /// <param name="cadence">When a merged change becomes a release.</param>
        public IWorkflowBuilder AddNuGet(string feed, ReleaseLine lines, ReleaseCadence cadence = ReleaseCadence.Curated)
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<INuGet, NuGetClient>();
            builder.Decorators.Decorate<INuGet, DryRunNuGet>();
            builder.Services.Configure<NuGetOptions>(o =>
            {
                o.Feed = feed;
                o.Lines = lines;
                o.Cadence = cadence;
            });
            builder.Services.Configure<NuGetOptions>(NuGetOptions.ConfigureFromEnvironment);
            return builder;
        }
    }
}
