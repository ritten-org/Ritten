using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;
using Ritten.Reporting;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class PipelineHostBuilder : IPipelineBuilder
{
    private readonly string _rootPath;
    private readonly SpectreProgressReporter _reporter;

    /// <summary>
    /// Creates a new instance of the <see cref="PipelineHostBuilder"/>.
    /// </summary>
    /// <param name="rootPath">The root of the project being built.</param>
    /// <param name="configuration">The configuration read from the repository.</param>
    /// <param name="reporter">The reporter that renders pipeline progress.</param>
    internal PipelineHostBuilder(string rootPath, IConfiguration configuration, SpectreProgressReporter reporter)
    {
        _rootPath = rootPath;
        _reporter = reporter;
        Services.AddSingleton(configuration);
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <inheritdoc />
    public IPipelineBuilder UseStep<TStep>() where TStep : class, IPipelineStep
    {
        Services.AddTransient<IPipelineStep, TStep>();
        return this;
    }

    /// <summary>
    /// Builds the <see cref="PipelineHost" />.
    /// </summary>
    /// <returns>The configured pipeline application.</returns>
    public PipelineHost Build()
    {
        Services.AddSingleton(TimeProvider.System);

        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();

        Services.TryAddSingleton<IFileSystem>(_ => new PhysicalFileSystem(_rootPath));
        Services.TryAddSingleton<IPipelineState, DefaultPipelineState>();

        Services.AddSingleton<IProgressReporter>(_reporter);
        Services.TryAddSingleton<IPipelineLog>(_reporter);

        var provider = Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        return new PipelineHost(provider);
    }
}
