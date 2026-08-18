using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Engine;
using Ritten.Workflows;

namespace Ritten.Changelogs;

/// <summary>
/// Registers the changelog domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds changelog checks.
        /// </summary>
        public IWorkflowBuilder AddChangelogs(ChangelogSettings settings)
        {
            builder.Services.TryAddSingleton<IChangelog, ChangelogClient>();
            builder.Services.Configure<ChangelogOptions>(o => o.File = settings.File);
            return builder;
        }
    }
}
