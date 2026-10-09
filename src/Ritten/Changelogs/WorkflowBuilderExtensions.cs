using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Engine;

namespace Ritten.Changelogs;

/// <summary>
/// Registers the changelog domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the changelog client.
        /// </summary>
        public IWorkflowBuilder AddChangelogs()
        {
            builder.Services.TryAddScoped<IChangelog, ChangelogClient>();
            builder.Decorators.Decorate<IChangelog, DryRunChangelog>();
            return builder;
        }
    }
}
