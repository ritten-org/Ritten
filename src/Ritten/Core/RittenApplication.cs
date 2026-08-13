using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ritten.Contracts;
using Ritten.Core.Runner;
using Ritten.Core.Steps;

namespace Ritten.Core;

/// <summary>
/// Represents a pipeline application that can be run.
/// </summary>
public class RittenApplication : IDisposable
{
    private readonly IHost _host;

    /// <summary>
    /// Creates a new instance of the <see cref="RittenApplication"/> class with the specified host.
    /// </summary>
    /// <param name="host">The host whose service container powers the pipeline.</param>
    internal RittenApplication(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// The service provider for the pipeline application.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Registers a step with the pipeline that will be run when the application is executed.
    /// Steps are resolved from the service provider and executed in the order they were added.
    /// </summary>
    /// <typeparam name="TStep">The type of the step to add. It must implement <see cref="IPipelineStep"/>.</typeparam>
    /// <returns>The current <see cref="RittenApplication"/> instance.</returns>
    public RittenApplication UseStep<TStep>() where TStep : class, IPipelineStep => UseStep(typeof(TStep));

    /// <summary>
    /// Registers a step with the pipeline that will be run when the application is executed.
    /// Steps are resolved from the service provider and executed in the order they were added.
    /// </summary>
    /// <param name="step">The type of the step to add. It must implement <see cref="IPipelineStep"/>.</param>
    /// <returns>The current <see cref="RittenApplication"/> instance.</returns>
    public RittenApplication UseStep(Type step)
    {
        var collector = _host.Services.GetService<IPipelineStepCollection>();
        if (collector == null)
        {
            throw new InvalidOperationException("This method of step registration is not supported when a custom IPipelineStepProvider has been configured.");
        }
        collector.AddStep(step);
        return this;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _host.Dispose();
    }

    /// <summary>
    /// Runs the pipeline and returns the exit code from the execution.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The exit code from the pipeline execution.</returns>
    public async Task<int> Run(CancellationToken cancellationToken = default)
    {
        try
        {
            var runner = Services.GetRequiredService<IPipelineRunner>();
            var summary = await runner.RunPipeline(cancellationToken);
            return summary.ExitCode;
        }
        finally
        {
            Dispose();
        }
    }

    /// <summary>
    /// Creates a new <see cref="RittenApplicationBuilder"/> with default settings.
    /// </summary>
    /// <returns>The created builder.</returns>
    public static RittenApplicationBuilder CreateBuilder() => new(new RittenApplicationOptions());

    /// <summary>
    /// Creates a new <see cref="RittenApplicationBuilder"/> with default settings.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    /// <returns>The created builder.</returns>
    public static RittenApplicationBuilder CreateBuilder(string[] args) => new(new RittenApplicationOptions { Args = args });

    /// <summary>
    /// Creates a new <see cref="RittenApplicationBuilder"/> with default settings.
    /// </summary>
    /// <param name="options">The options to pass the pipeline builder.</param>
    /// <returns>The created builder.</returns>
    public static RittenApplicationBuilder CreateBuilder(RittenApplicationOptions options) => new(options);
}
