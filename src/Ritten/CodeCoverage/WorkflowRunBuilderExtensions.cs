using Microsoft.Extensions.DependencyInjection;
using Ritten.Core;
using Ritten.Workflows;

namespace Ritten.CodeCoverage;

/// <summary>
/// Registers the coverage domain.
/// </summary>
public static class WorkflowRunBuilderExtensions
{
    extension(WorkflowRunBuilder builder)
    {
        /// <summary>
        /// Adds coverage collection and thresholds, configured from the project's settings.
        /// </summary>
        public WorkflowRunBuilder AddCoverage(CoverageSettings settings)
        {
            builder.Services.Configure<CoverageOptions>(o =>
            {
                o.MinimumLine = settings.Line;
                o.MinimumBranch = settings.Branch;
            });
            return builder;
        }
    }
}
