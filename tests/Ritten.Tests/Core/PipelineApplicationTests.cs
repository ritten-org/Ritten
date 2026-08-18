using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.GitHub;
using Ritten.Pipelines.DotNetTool;
using Ritten.Tests.Support;

namespace Ritten.Tests.Core;

public class PipelineApplicationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-application-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public void CreateBuilder_RegistersByType()
    {
        var builder = PipelineApplication.CreateBuilder();
        builder.Pipelines.Add<DotNetToolPipeline>();
        builder.Runtimes.Add<GitHubActionsRuntime>();

        builder.Build().IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Build_JudgesTheWholeRegisteredModel()
    {
        var builder = PipelineApplication.CreateBuilder();
        builder.Pipelines.Add(new TestPipeline("same"));
        builder.Pipelines.Add(new TestPipeline("same"));

        var application = builder.Build();

        application.IsError.ShouldBeTrue();
        application.Errors.ShouldHaveSingleItem().Message.ShouldBe("Two pipelines are registered under the name 'same'.");
    }

    [Fact]
    public async Task Run_RunsTheDeclaredJobWithTheSharedServices()
    {
        // The probe reaches the step only through the application's shared services, so this run
        // proves the whole path: resolve the project, select pipeline and job, assemble, execute.
        WriteRittenJson("""{ "pipeline": "test" }""");
        var probe = new StepProbe();
        var builder = PipelineApplication.CreateBuilder();
        builder.Pipelines.Add(new TestPipeline(jobs: [new TestJob(steps: [Step.FromType<ProbeStep>()])]));
        builder.Services.AddSingleton(probe);
        builder.Services.AddSingleton(Substitute.For<IPipelineLog>());
        var application = builder.Build().Value.ShouldNotBeNull();

        var exitCode = await application.Run(new RunJobArgs("verify") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(PipelineExitCodes.Success);
        probe.Ran.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Run_ReportsAJobThePipelineDoesNotDeclare()
    {
        WriteRittenJson("""{ "pipeline": "test" }""");
        var application = Application(new TestPipeline());

        var exitCode = await application.Run(new RunJobArgs("deploy") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(PipelineExitCodes.ConfigurationError);
    }

    [Fact]
    public async Task Run_ReportsAPipelineTheApplicationDoesNotKnow()
    {
        WriteRittenJson("""{ "pipeline": "imaginary" }""");
        var application = Application(new TestPipeline());

        var exitCode = await application.Run(new RunJobArgs("verify") { Directory = _root }, Empty, TestContext.Current.CancellationToken);

        exitCode.ShouldBe(PipelineExitCodes.ConfigurationError);
    }

    private static Func<string, string?> Empty { get; } = _ => null;

    private static PipelineApplication Application(TestPipeline pipeline)
    {
        var builder = PipelineApplication.CreateBuilder();
        builder.Pipelines.Add(pipeline);
        return builder.Build().Value.ShouldNotBeNull();
    }

    private void WriteRittenJson(string content)
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "ritten.json"), content);
    }
}
