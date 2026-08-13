using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Steps;

namespace Ritten.Core;

internal class PipelineBuilder(IServiceCollection services) : IPipelineBuilder
{
    private readonly List<Type> _stepTypes = [];

    public IServiceCollection Services { get; } = services;

    public IPipelineBuilder UseStep<TStep>() where TStep : class, IPipelineStep
    {
        _stepTypes.Add(typeof(TStep));
        Services.AddTransient<TStep>();
        return this;
    }

    public PipelineStepTypes BuildStepTypes() => new(_stepTypes);
}
