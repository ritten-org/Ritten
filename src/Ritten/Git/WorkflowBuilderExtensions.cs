using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Core;

namespace Ritten.Git;

/// <summary>
/// Registers the git domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds release tagging, configured from the project's settings.
        /// </summary>
        public IWorkflowBuilder AddGit(string tagPrefix)
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IGit, GitClient>();
            builder.Decorators.Decorate<IGit, DryRunGit>();
            builder.Services.Configure<GitOptions>(o => o.TagPrefix = tagPrefix);
            builder.Services.Configure<GitOptions>(GitOptions.ConfigureFromEnvironment);
            return builder;
        }
    }
}
