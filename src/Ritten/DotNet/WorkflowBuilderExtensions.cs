using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;
using Ritten.Workflows;

namespace Ritten.DotNet;

/// <summary>
/// Registers the .NET domain.
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
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IDotNet, DotNetClient>();
            builder.Services.Configure<DotNetOptions>(o =>
            {
                // `project` and `projects` are two spellings of one setting; validation has
                // already refused files that use both.
                var projects = settings.Projects is { Count: > 0 }
                    ? settings.Projects
                    : settings.Project is null ? [] : [settings.Project];
                o.Configuration = settings.Configuration;
                o.ProjectFile = projects.FirstOrDefault() ?? "";
                o.Repository = repository;
                o.Projects = projects;
            });
            return builder;
        }
    }
}
