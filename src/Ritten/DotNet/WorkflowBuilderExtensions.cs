using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Core;
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
                o.Configuration = settings.Configuration;
                o.ProjectFile = settings.Project ?? "";
                o.Repository = repository;
            });
            return builder;
        }
    }
}
