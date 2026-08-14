using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class RittenApplicationBuilder : IPipelineBuilder
{
    /// <summary>
    /// Creates a new instance of the <see cref="RittenApplicationBuilder"/>.
    /// </summary>
    internal RittenApplicationBuilder()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        Services.AddSingleton<IConfiguration>(configuration);
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
    /// Builds the <see cref="RittenApplication" />.
    /// </summary>
    /// <returns>The configured pipeline application.</returns>
    public RittenApplication Build()
    {
        Services.AddSingleton(TimeProvider.System);

        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();

        Services.TryAddSingleton<IFileSystem>(_ => new PhysicalFileSystem(Environment.CurrentDirectory));
        Services.TryAddSingleton<IPipelineState, DefaultPipelineState>();

        var reporter = new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail);
        Services.AddSingleton<IProgressReporter>(reporter);
        Services.TryAddSingleton<IPipelineLog>(reporter);

        var provider = Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        return new RittenApplication(provider);
    }
}
