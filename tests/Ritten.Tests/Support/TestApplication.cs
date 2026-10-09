using System.Text.Json;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;

namespace Ritten.Tests.Support;

/// <summary>
/// Builds and runs an application around jobs a test declares, as the one workflow it runs.
/// </summary>
internal static class TestApplication
{
    /// <summary>
    /// Builds the application, whether or not its model holds together.
    /// </summary>
    /// <param name="jobs">The jobs of its one workflow.</param>
    /// <param name="configure">Registers whatever else the test needs.</param>
    /// <param name="runtime">The runtime it finds itself in; a <see cref="TestRuntime"/> unless given.</param>
    /// <param name="environment">The environment the runtime is detected from; the test runtime's marker unless given.</param>
    /// <param name="fileName">The project file name the application reads.</param>
    public static Result<WorkflowApplication> Build(
        IEnumerable<IJob> jobs,
        Action<WorkflowApplicationBuilder>? configure = null,
        Runtime? runtime = null,
        Func<string, string?>? environment = null,
        string fileName = RittenProject.DefaultFileName)
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.ProjectFileName = fileName;
        builder.Workflows.Add(new TestWorkflow("test", [.. jobs], label: "Test"));
        builder.Runtimes.Add(runtime ?? new TestRuntime());
        configure?.Invoke(builder);
        return builder.Build(environment ?? TestRuntime.Environment);
    }

    /// <summary>
    /// Builds the application, which the test expects to hold together.
    /// </summary>
    public static WorkflowApplication Create(
        IEnumerable<IJob> jobs,
        Action<WorkflowApplicationBuilder>? configure = null,
        Runtime? runtime = null,
        Func<string, string?>? environment = null,
        string fileName = RittenProject.DefaultFileName) =>
        Build(jobs, configure, runtime, environment, fileName).Value.ShouldNotBeNull();

    /// <summary>
    /// Runs one of the application's jobs the way the command line does, in a project whose file holds the given settings.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="job">The job's name.</param>
    /// <param name="settings">The project file's contents.</param>
    /// <param name="dryRun">Whether the run is a rehearsal.</param>
    /// <param name="options">The values given on the command line.</param>
    /// <param name="fileName">The project file's name.</param>
    public static Task<ExitCode> Run(
        this WorkflowApplication application,
        string job = "verify",
        string settings = "{}",
        bool dryRun = false,
        IReadOnlyDictionary<JobOption, object?>? options = null,
        string fileName = RittenProject.DefaultFileName)
    {
        var project = new RittenProject
        {
            Directory = Path.GetTempPath(),
            FileName = fileName,
            Settings = JsonSerializer.Deserialize<JsonElement>(settings)
        };

        return application.Run(
            new SelectedWorkflow(application.Workflows[0], project),
            new RunJobArgs(job) { DryRun = dryRun, Options = options ?? new Dictionary<JobOption, object?>() },
            TestContext.Current.CancellationToken);
    }
}
