using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Engine.Workflows;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.Helpers;

internal static class DefaultWorkflowRunnerHelpers
{
    public static DefaultWorkflowRunner CreateRunner(
        object[]? steps = null,
        IWorkflowProgress[]? reporters = null,
        WorkflowJob? job = null,
        IWorkflowLog? log = null,
        RunContext? context = null
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

        var run = new RunState();
        run.Start(new Run(
            new SelectedWorkflow(new TestWorkflow(), RittenProject.At(Path.GetTempPath(), RittenProject.DefaultFileName)),
            new TestJob<NoArguments>(steps: methods),
            NoArguments.Instance,
            new RunOptions(),
            new RecordingConsole()));

        return new DefaultWorkflowRunner(
            log,
            reporters ?? [],
            methods,
            services.BuildServiceProvider(),
            job,
            context ?? new RunContext(),
            run);
    }
}
