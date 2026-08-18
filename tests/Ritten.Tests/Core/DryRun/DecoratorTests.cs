using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Tests.Core.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Core.DryRun;

public class DecoratorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-decorators-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task DryRun_StopsSideEffectsAtTheDecorator()
    {
        var client = new RealClient();
        using var run = Build(dryRun: true, builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.DryRun.Decorate<IOutwardClient, RehearsingClient>();
        });

        await run.Run(TestContext.Current.CancellationToken);

        client.Pushes.ShouldBe(0, "the rehearsal decorator must swallow the side effect");
    }

    [Fact]
    public async Task DryRun_SubstitutesTheReplacement()
    {
        var client = new RealClient();
        using var run = Build(dryRun: true, builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.DryRun.Replace<IOutwardClient, NullClient>();
        });

        await run.Run(TestContext.Current.CancellationToken);

        client.Pushes.ShouldBe(0);
    }

    [Fact]
    public async Task Run_LeavesTheDecoratedClientAloneOutsideADryRun()
    {
        // The decorator is a declaration, not a decoration: a real run reaches the real client.
        var client = new RealClient();
        using var run = Build(dryRun: false, builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.DryRun.Decorate<IOutwardClient, RehearsingClient>();
        });

        await run.Run(TestContext.Current.CancellationToken);

        client.Pushes.ShouldBe(1);
    }

    [Fact]
    public void DryRun_IgnoresADecoratorWhoseClientIsNotRegistered()
    {
        // A workflow only registers the capabilities it uses; a decorator for an absent client
        // is a no-op, not an error.
        var builder = WorkflowRunBuilderHelpers.Create(dryRun: true);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.DryRun.Decorate<IOutwardClient, RehearsingClient>();

        var result = builder.Build(new TestJob());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Dispose();
    }

    [Fact]
    public async Task Run_AppliesTheApplicationsSharedDecoratorsInADryRun()
    {
        // The application's decorators travel to the run through WithDecorators; a shared client
        // declared once at application level must stay rehearsal-safe in every job.
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(Path.Combine(_root, "ritten.json"), """{ "workflow": "test" }""", TestContext.Current.CancellationToken);
        var client = new RealClient();
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow(jobs: [new TestJob(steps: [Step.FromType<PushStep>()])]));
        builder.Services.AddSingleton<IOutwardClient>(client);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.DryRun.Decorate<IOutwardClient, RehearsingClient>();
        var application = builder.Build().Value.ShouldNotBeNull();

        var args = new RunJobArgs("verify") { Directory = _root, DryRun = true };
        var exitCode = await application.Run(args, _ => null, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(WorkflowExitCodes.Success);
        client.Pushes.ShouldBe(0, "an application-level decorator must reach the run");
    }

    private static WorkflowRun Build(bool dryRun, Action<WorkflowRunBuilder> configure)
    {
        var builder = WorkflowRunBuilderHelpers.Create(dryRun: dryRun);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        configure(builder);

        var result = builder.Build(new TestJob(steps: [Step.FromType<PushStep>()]));
        result.IsSuccess.ShouldBeTrue();
        return result.Value.ShouldNotBeNull();
    }

    private interface IOutwardClient
    {
        void Push();
    }

    private sealed class RealClient : IOutwardClient
    {
        public int Pushes { get; private set; }

        public void Push() => Pushes++;
    }

    private sealed class RehearsingClient(IOutwardClient inner) : IOutwardClient
    {
        // Holding the real client without pushing through it is the decorator contract: reads
        // would pass through, side effects stop here.
        public void Push() => _ = inner;
    }

    private sealed class NullClient : IOutwardClient
    {
        public void Push()
        {
        }
    }

    [Step("push", StepKind.Work)]
    private sealed class PushStep(IOutwardClient client)
    {
        public StepResult Run()
        {
            client.Push();
            return StepResult.Successful;
        }
    }
}
