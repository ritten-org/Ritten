using Ritten.Engine;

namespace Ritten.Npm;

/// <summary>
/// Registers the npm domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the npm client. Installing, linting, building and testing change nothing outside
        /// the project, so a rehearsal runs them as they are.
        /// </summary>
        public IWorkflowBuilder AddNpm()
        {
            builder.Services.AddNpm();
            return builder;
        }
    }
}
