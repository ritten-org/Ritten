using Microsoft.Extensions.DependencyInjection;
using Ritten.Engine;

namespace Ritten.Init;

/// <summary>
/// Registers what a job that sets a repository up needs.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the tool the repository is being set up to run.
        /// </summary>
        /// <param name="tool">The tool being pinned, which is the one doing the setting up.</param>
        public IWorkflowBuilder AddInit(ToolPin tool)
        {
            builder.Services.AddSingleton(tool);
            return builder;
        }
    }
}
