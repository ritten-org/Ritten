using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;

namespace Ritten.Git;

/// <summary>
/// Registers the git domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the git client.
        /// </summary>
        public IWorkflowBuilder AddGit()
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IGit, GitClient>();
            builder.Decorators.Decorate<IGit, DryRunGit>();
            return builder;
        }

        /// <summary>
        /// Adds the git client and configures release tagging. The prefix belongs to the tagging
        /// steps, not the client, so hosts that never tag call the parameterless overload.
        /// </summary>
        public IWorkflowBuilder AddGit(string tagPrefix)
        {
            builder.AddGit();
            builder.Services.Configure<GitOptions>(o => o.TagPrefix = tagPrefix);
            builder.Services.Configure<GitOptions>(GitOptions.ConfigureFromEnvironment);
            return builder;
        }
    }
}
