using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultWorkflowRunnerHelpers
{
    public static DefaultWorkflowRunner CreateRunner(
        object[]? steps = null,
        IProgressReporter[]? reporters = null,
        WorkflowJob? job = null,
        IWorkflowLog? log = null
    )
    {
        log ??= Substitute.For<IWorkflowLog>();
        job ??= new WorkflowJob("Test", "verify");
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

        return new DefaultWorkflowRunner(
            log,
            reporters ?? [],
            methods,
            services.BuildServiceProvider(),
            job);
    }
}
