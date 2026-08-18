using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runtimes;

namespace Ritten.Core;

/// <summary>
/// Configures a <see cref="PipelineApplication"/>.
/// </summary>
public sealed class PipelineApplicationBuilder
{
    internal PipelineApplicationBuilder()
    {
    }

    /// <summary>
    /// The pipelines the application can run.
    /// </summary>
    public PipelineRegistry Pipelines { get; } = new();

    /// <summary>
    /// The runtimes the application can find itself running in.
    /// </summary>
    public RuntimeRegistry Runtimes { get; } = new();

    /// <summary>
    /// Services registered for every job of every pipeline.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// Builds and validates the pipeline application.
    /// </summary>
    public Result<PipelineApplication> Build()
    {
        List<Error> model = [.. Pipelines.Validate(), .. Runtimes.Validate()];
        if (model.Count > 0)
        {
            // Narrated here, so a host's entry point can map the failure straight to an exit
            // code without owning any reporting of its own.
            PipelineApplication.EngineConsole().Errors(model);
            return model;
        }

        return new PipelineApplication(Pipelines, Runtimes, [.. Services]);
    }
}
