using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.Extensions;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;
using Ritten.Core.Steps;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class RittenApplicationBuilder : IHostApplicationBuilder
{
    private readonly HostApplicationBuilder _innerBuilder;

    /// <summary>
    /// Creates a new instance of the <see cref="RittenApplicationBuilder"/> with the given options.
    /// </summary>
    /// <param name="options">The options to configure the pipeline application.</param>
    internal RittenApplicationBuilder(RittenApplicationOptions options)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Logging:LogLevel:Microsoft.Hosting.Lifetime", nameof(LogLevel.Warning) }
        });

        var contentRoot = options.ContentRootPath ?? AppContext.BaseDirectory;

        _innerBuilder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = options.Args,
            ApplicationName = options.ApplicationName,
            EnvironmentName = options.EnvironmentName,
            ContentRootPath = contentRoot,
            Configuration = configuration,
        });

        Logging.AddPipelineConsoleFormatter();
    }

    /// <inheritdoc />
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)_innerBuilder).Properties;

    /// <inheritdoc />
    public IConfigurationManager Configuration => _innerBuilder.Configuration;

    /// <inheritdoc />
    public IHostEnvironment Environment => _innerBuilder.Environment;

    /// <inheritdoc />
    public ILoggingBuilder Logging => _innerBuilder.Logging;

    /// <inheritdoc />
    public IMetricsBuilder Metrics => _innerBuilder.Metrics;

    /// <inheritdoc />
    public IServiceCollection Services => _innerBuilder.Services;

    /// <summary>
    /// Builds the <see cref="RittenApplication" />.
    /// </summary>
    /// <returns>The configured pipeline application.</returns>
    public RittenApplication Build()
    {
        ApplyServices(Services);

        var host = _innerBuilder.Build();
        return new RittenApplication(host);
    }

    /// <inheritdoc />
    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null
    ) where TContainerBuilder : notnull => _innerBuilder.ConfigureContainer(factory, configure);

    private static void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();
        services.TryAddSingleton<IPipelineStepRunner, DefaultPipelineStepRunner>();

        services.TryAddScoped<IFileSystem>(_ => new PhysicalFileSystem(System.Environment.CurrentDirectory));
        services.TryAddScoped<IPipelineState, DefaultPipelineState>();
        services.TryAddScoped<IPipelineContext, DefaultPipelineContext>();

        var hasProvider = services.Any(d => d.ServiceType == typeof(IPipelineStepProvider));
        if (!hasProvider)
        {
            services.TryAddSingleton<IPipelineStepCollection, PipelineStepCollection>();
            services.TryAddScoped<IPipelineStepProvider, PipelineStepProvider>();
        }

        services.AddGitHubActionsRuntime();
    }
}
