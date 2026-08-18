using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Core;
using Ritten.Workflows;

namespace Ritten.Changelogs;

/// <summary>
/// Registers the changelog domain.
/// </summary>
public static class WorkflowRunBuilderExtensions
{
    extension(WorkflowRunBuilder builder)
    {
        /// <summary>
        /// Adds changelog checks.
        /// </summary>
        public WorkflowRunBuilder AddChangelogs(ChangelogSettings settings)
        {
            builder.Services.TryAddSingleton<IChangelog, ChangelogClient>();
            builder.Services.Configure<ChangelogOptions>(o => o.File = settings.File);
            return builder;
        }
    }
}
