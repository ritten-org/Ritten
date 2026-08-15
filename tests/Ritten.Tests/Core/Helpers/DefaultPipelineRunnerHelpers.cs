using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultPipelineRunnerHelpers
{
    public static DefaultPipelineRunner CreateRunner(
        object[]? steps = null,
        IProgressReporter[]? reporters = null,
        PipelineJob? job = null,
        IPipelineLog? log = null
    )
    {
        log ??= Substitute.For<IPipelineLog>();
        job ??= new PipelineJob("Test", "verify");
        steps ??= [];

        // The instances are registered directly, so the runner resolves exactly the steps the
        // test configured.
        var services = new ServiceCollection();
        var methods = new List<StepDescriptor>();
        foreach (var step in steps)
        {
            if (StepDescriptor.Describe(step.GetType()).Value is not { } method)
            {
                throw new InvalidOperationException($"{step.GetType().Name} has an invalid Run method.");
            }

            methods.Add(method);
            services.AddSingleton(step.GetType(), step);
        }

        return new DefaultPipelineRunner(
            log,
            reporters ?? [],
            methods,
            services.BuildServiceProvider(),
            job);
    }
}
