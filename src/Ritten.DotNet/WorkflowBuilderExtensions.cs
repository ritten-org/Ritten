using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Engine;

namespace Ritten.DotNet;

/// <summary>
/// Registers the .NET domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the .NET client, building the given projects with the given configuration.
        /// </summary>
        /// <param name="projects">The project files to work on; the first is the metadata source.</param>
        /// <param name="configuration">The build configuration used to build, test, and pack.</param>
        /// <param name="repository">The repository URL packages carry, when the host knows it.</param>
        public IWorkflowBuilder AddDotNet(IReadOnlyList<string> projects, string configuration, string? repository = null)
        {
            builder.AddCommandRunner();
            builder.Services.TryAddSingleton<IDotNet, DotNetClient>();
            builder.Decorators.Decorate<IDotNet, DryRunDotNet>();
            builder.Services.Configure<DotNetOptions>(o =>
            {
                o.Configuration = configuration;
                o.ProjectFile = projects.FirstOrDefault() ?? "";
                o.Repository = repository;
                o.Projects = projects;
            });
            return builder;
        }
    }
}
