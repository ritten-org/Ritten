using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class RittenApplicationBuilder : IPipelineBuilder
{
    private readonly HostApplicationBuilder _innerBuilder;
    /// <summary>
    /// Creates a new instance of the <see cref="RittenApplicationBuilder"/> with the given options.
    /// </summary>
    /// <param name="options">The options to configure the pipeline application.</param>
    internal RittenApplicationBuilder(RittenApplicationOptions options)
    {
        _innerBuilder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = options.Args,
            ApplicationName = options.ApplicationName,
            EnvironmentName = options.EnvironmentName,
            ContentRootPath = options.ContentRootPath ?? AppContext.BaseDirectory,
            Configuration = new ConfigurationManager(),
        });
    }

    /// <inheritdoc />
    public IServiceCollection Services => _innerBuilder.Services;

    /// <inheritdoc />
    public IPipelineBuilder UseStep<TStep>() where TStep : class, IPipelineStep
    {
        Services.AddTransient<IPipelineStep, TStep>();
        return this;
    }

    /// <summary>
    /// Builds the <see cref="RittenApplication" />.
    /// </summary>
    /// <returns>The configured pipeline application.</returns>
    public RittenApplication Build()
    {
        Services.AddSingleton(TimeProvider.System);

        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();

        Services.TryAddSingleton<IFileSystem>(_ => new PhysicalFileSystem(Environment.CurrentDirectory));
        Services.TryAddSingleton<IPipelineState, DefaultPipelineState>();
        Services.TryAddSingleton<IPipelineContext, DefaultPipelineContext>();

        var host = _innerBuilder.Build();
        return new RittenApplication(host);
    }
}
