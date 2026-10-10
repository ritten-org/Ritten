using Microsoft.Extensions.DependencyInjection;
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
        /// Adds the git client, and the commit to tag from the environment.
        /// </summary>
        public IWorkflowBuilder AddGit()
        {
            builder.Services.AddGit();
            builder.Services.AddOptions<GitOptions>().Configure(GitOptions.ConfigureFromEnvironment);
            builder.Decorators.Decorate<IGit, DryRunGit>();
            return builder;
        }
    }
}
