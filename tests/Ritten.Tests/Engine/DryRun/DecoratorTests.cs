using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Engine.DryRun;

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
        using var application = Build(builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.Decorators.Decorate<IOutwardClient, RehearsingClient>();
        });

        await application.Run(dryRun: true);

        client.Pushes.ShouldBe(0, "the rehearsal decorator must swallow the side effect");
    }

    [Fact]
    public async Task DryRun_SubstitutesTheReplacement()
    {
        var client = new RealClient();
        using var application = Build(builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.Decorators.Replace<IOutwardClient, NullClient>();
        });

        await application.Run(dryRun: true);

        client.Pushes.ShouldBe(0);
    }

    [Fact]
    public async Task Run_LeavesTheDecoratedClientAloneOutsideADryRun()
    {
        // The decorator is a declaration, not a decoration: a real run reaches the real client.
        var client = new RealClient();
        using var application = Build(builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.Decorators.Decorate<IOutwardClient, RehearsingClient>();
        });

        await application.Run(dryRun: false);

        client.Pushes.ShouldBe(1);
    }

    [Fact]
    public async Task Run_ChoosesTheClientForEachRunOfOneApplication()
    {
        // The pairing is decided as each run resolves the client, so a rehearsal and a real run can share an application.
        var client = new RealClient();
        using var application = Build(builder =>
        {
            builder.Services.AddSingleton<IOutwardClient>(client);
            builder.Decorators.Decorate<IOutwardClient, RehearsingClient>();
        });

        await application.Run(dryRun: true);
        await application.Run(dryRun: false);

        client.Pushes.ShouldBe(1);
    }

    [Fact]
    public void DryRun_IgnoresADecoratorWhoseClientIsNotRegistered()
    {
        // A workflow only registers the capabilities it uses; a decorator for an absent client
        // is a no-op, not an error.
        var result = TestApplication.Build([new TestJob()], builder => builder.Decorators.Decorate<IOutwardClient, RehearsingClient>());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Dispose();
    }

    [Fact]
    public async Task Run_AppliesTheApplicationsSharedDecoratorsInADryRun()
    {
        // A shared client declared once at application level must stay rehearsal-safe in every job.
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(Path.Combine(_root, "ritten.json"), """{ "workflow": "test" }""", TestContext.Current.CancellationToken);
        var client = new RealClient();
        var builder = WorkflowApplication.CreateBuilder();
        builder.Workflows.Add(new TestWorkflow(jobs: [new TestJob(steps: [Step.FromType<PushStep>()])]));
        builder.Services.AddSingleton<IOutwardClient>(client);
        builder.Services.AddSingleton(Substitute.For<IWorkflowLog>());
        builder.Decorators.Decorate<IOutwardClient, RehearsingClient>();
        var application = builder.Build().Value.ShouldNotBeNull();

        var selection = await application.SelectWorkflow(_root, ct: TestContext.Current.CancellationToken);
        var args = new RunJobArgs("verify") { DryRun = true };
        var exitCode = await application.Run(selection, args, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(ExitCode.Success);
        client.Pushes.ShouldBe(0, "an application-level decorator must reach the run");
    }

    private static WorkflowApplication Build(Action<WorkflowApplicationBuilder> configure) =>
        TestApplication.Create([new TestJob(steps: [Step.FromType<PushStep>()])], configure);

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
