using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
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
        var methods = new List<Step>();
        foreach (var step in steps)
        {
            methods.Add(Step.FromType(step.GetType()));
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
