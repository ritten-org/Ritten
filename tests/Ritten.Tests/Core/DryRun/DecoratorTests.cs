using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Tests.Core.Helpers;
using Ritten.Tests.Support;

namespace Ritten.Tests.Core.DryRun;

public class DecoratorTests
{
    [Fact]
    public async Task DryRun_StopsSideEffectsAtThePairedDecorator()
    {
        var client = new RealClient();
        using var run = Build(dryRun: true, services =>
        {
            services.AddSingleton<IOutwardClient>(client);
            services.DryRun.Decorate<IOutwardClient, RehearsingClient>();
        });

        await run.Run(TestContext.Current.CancellationToken);

        client.Pushes.ShouldBe(0, "the rehearsal decorator must swallow the side effect");
    }

    [Fact]
    public async Task DryRun_SubstitutesThePairedReplacement()
    {
        var client = new RealClient();
        using var run = Build(dryRun: true, services =>
        {
            services.AddSingleton<IOutwardClient>(client);
            services.DryRun.Replace<IOutwardClient, NullClient>();
        });

        await run.Run(TestContext.Current.CancellationToken);

        client.Pushes.ShouldBe(0);
    }

    [Fact]
    public async Task Run_LeavesThePairedClientAloneOutsideADryRun()
    {
        // The pairing is a declaration, not a decoration: a real run reaches the real client.
        var client = new RealClient();
        using var run = Build(dryRun: false, services =>
        {
            services.AddSingleton<IOutwardClient>(client);
            services.DryRun.Decorate<IOutwardClient, RehearsingClient>();
        });

        await run.Run(TestContext.Current.CancellationToken);

        client.Pushes.ShouldBe(1);
    }

    [Fact]
    public void DryRun_IgnoresAPairingWhoseClientIsNotRegistered()
    {
        // A workflow only registers the capabilities it uses; a pairing for an absent client is
        // a no-op, not an error.
        var builder = WorkflowRunBuilderHelpers.Create(dryRun: true);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.DryRun.Decorate<IOutwardClient, RehearsingClient>();

        var result = builder.Build(new TestJob());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Dispose();
    }

    private static WorkflowRun Build(bool dryRun, Action<IServiceCollection> configure)
    {
        var builder = WorkflowRunBuilderHelpers.Create(dryRun: dryRun);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        configure(builder.Services);

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
