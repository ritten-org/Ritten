using Ritten.DotNet;
using Ritten.Engine;

namespace Ritten.Workflows;

/// <summary>
/// Registers domains from the project's settings — the bridge between the settings records this
/// tool defines and the domain packages, which know nothing of <c>ritten.json</c>.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the .NET client and build settings, configured from the project's settings.
        /// </summary>
        public IWorkflowBuilder AddDotNet(DotNetBuildSettings settings, string? repository = null)
        {
            // `project` and `projects` are two spellings of one setting; validation has
            // already refused files that use both.
            var projects = settings.Projects is { Count: > 0 }
                ? settings.Projects
                : settings.Project is null ? [] : [settings.Project];
            return builder.AddDotNet(projects, settings.Configuration, repository);
        }
    }
}
