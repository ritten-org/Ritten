using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Commands;
using Ritten.Workflows;

namespace Ritten.DotNet;

/// <summary>
/// Registers the .NET domain.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the .NET client and build settings, configured from the project's settings.
        /// </summary>
        public IServiceCollection AddDotNet(DotNetBuildSettings settings, string? repository = null)
        {
            services.AddCommandRunner();
            services.TryAddSingleton<IDotNet, DotNetClient>();
            services.Configure<DotNetOptions>(o =>
            {
                o.Configuration = settings.Configuration;
                o.ProjectFile = settings.Project ?? "";
                o.Repository = repository;
            });
            return services;
        }
    }
}
